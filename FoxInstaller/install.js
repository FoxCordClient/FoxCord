const { app, BrowserWindow, shell, nativeTheme } = require("electron");
const path = require("path");
const fs = require("fs");
const https = require("https");
const os = require("os");
const { spawn, exec, execSync } = require("child_process");
const AdmZip = require("adm-zip");

const GITHUB_RELEASE_API = "https://api.github.com/repos/FoxCordClient/FoxCord/releases/latest";
const ZIP_DOWNLOAD_URL = "https://github.com/FoxCordClient/FoxCord/releases/latest/download/fxcd.zip";
const FOXCORD_DIR = path.join(process.env.LOCALAPPDATA || path.join(os.homedir(), "AppData", "Local"), "FoxCord");

// Força o tema escuro nativo nas janelas
nativeTheme.themeSource = "dark";

/**
 * Envia mensagens de status e porcentagem para a interface do Electron.
 */
function updateUI(win, message, percent = -1) {
    if (win && !win.webContents.isDestroyed()) {
        win.webContents.send("update-status", { message, percent });
    }
}

/**
 * Força o fechamento do FoxCord.exe antes de iniciar a atualização.
 */
function killFoxCordProcess() {
    return new Promise((resolve) => {
        if (process.platform === "win32") {
            exec('taskkill /F /IM FoxCord.exe /T', () => resolve());
        } else {
            exec('pkill -f FoxCord', () => resolve());
        }
    });
}

/**
 * Função utilitária para copiar pastas e subpastas de forma robusta.
 */
function copyDirSync(src, dest) {
    if (!fs.existsSync(dest)) fs.mkdirSync(dest, { recursive: true });
    const entries = fs.readdirSync(src, { withFileTypes: true });
    
    for (const entry of entries) {
        const srcPath = path.join(src, entry.name);
        const destPath = path.join(dest, entry.name);
        if (entry.isDirectory()) {
            copyDirSync(srcPath, destPath);
        } else {
            fs.copyFileSync(srcPath, destPath);
        }
    }
}

/**
 * (C# Port) Faz o backup da pasta do WebView2 antes de deletar as antigas.
 */
function backupWebView2Data(baseDir, tempBackupPath) {
    let foundPath = null;
    if (fs.existsSync(baseDir)) {
        const entries = fs.readdirSync(baseDir, { withFileTypes: true });
        for (const entry of entries) {
            if (entry.isDirectory() && entry.name.startsWith("app-")) {
                const possiblePath = path.join(baseDir, entry.name, "FoxCord.exe.WebView2");
                if (fs.existsSync(possiblePath)) {
                    foundPath = possiblePath;
                    break;
                }
            }
        }
    }

    if (foundPath) {
        try {
            if (fs.existsSync(tempBackupPath)) {
                fs.rmSync(tempBackupPath, { recursive: true, force: true });
            }
            copyDirSync(foundPath, tempBackupPath);
            console.log("Backup do WebView2 feito com sucesso.");
        } catch (err) {
            console.error("Aviso: Falha ao fazer backup da pasta WebView2:", err.message);
        }
    }
}

/**
 * (C# Port) Restaura a pasta de dados do WebView2 para a versão nova.
 */
function restoreWebView2Data(tempBackupPath, targetAppDir) {
    if (fs.existsSync(tempBackupPath)) {
        try {
            const targetPath = path.join(targetAppDir, "FoxCord.exe.WebView2");
            copyDirSync(tempBackupPath, targetPath);
            fs.rmSync(tempBackupPath, { recursive: true, force: true });
            console.log("Dados do WebView2 restaurados com sucesso.");
        } catch (err) {
            console.error("Aviso: Falha ao restaurar os dados do WebView2:", err.message);
        }
    }
}

/**
 * (C# Port) Recria os Atalhos (Menu Iniciar e Desktop).
 */
function createShortcuts(exePath) {
    if (process.platform !== "win32") return;
    try {
        const startMenuPrograms = path.join(app.getPath("appData"), "Microsoft", "Windows", "Start Menu", "Programs");
        const foxCordMenuDir = path.join(startMenuPrograms, "FoxCord");
        
        if (!fs.existsSync(foxCordMenuDir)) fs.mkdirSync(foxCordMenuDir, { recursive: true });

        // Menu Iniciar
        const startMenuPath = path.join(foxCordMenuDir, "FoxCord.lnk");
        shell.writeShortcutLink(startMenuPath, "create", {
            target: exePath,
            cwd: path.dirname(exePath),
            description: "FoxCord",
            icon: exePath,
            iconIndex: 0
        });

        // Desktop
        const desktopPath = path.join(app.getPath("desktop"), "FoxCord.lnk");
        shell.writeShortcutLink(desktopPath, "create", {
            target: exePath,
            cwd: path.dirname(exePath),
            description: "FoxCord",
            icon: exePath,
            iconIndex: 0
        });
    } catch (err) {
        console.error("Falha ao criar atalhos:", err.message);
    }
}

/**
 * (C# Port) Atualiza o Registro para mostrar a versão no Painel de Controle (Desinstalador).
 */
function createRegistryUninstaller(baseDir, exePath, version) {
    if (process.platform !== "win32") return;
    try {
        const regKey = `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\FoxCord`;
        const commands = [
            `reg add "${regKey}" /v DisplayName /d "FoxCord" /f`,
            `reg add "${regKey}" /v DisplayVersion /d "${version}" /f`,
            `reg add "${regKey}" /v Publisher /d "FoxCord" /f`,
            `reg add "${regKey}" /v DisplayIcon /d "${exePath}" /f`,
            `reg add "${regKey}" /v InstallLocation /d "${baseDir}" /f`,
            `reg add "${regKey}" /v UninstallString /d "cmd.exe /c rmdir /s /q \\"${baseDir}\\"" /f`,
            `reg add "${regKey}" /v NoModify /t REG_DWORD /d 1 /f`,
            `reg add "${regKey}" /v NoRepair /t REG_DWORD /d 1 /f`
        ];
        
        for (const cmd of commands) {
            execSync(cmd, { stdio: 'ignore' });
        }
    } catch (err) {
        console.error("Aviso: Falha ao registrar desinstalador:", err.message);
    }
}

/**
 * Consulta a API do GitHub para obter a versão atual e o link dinâmico.
 */
function fetchLatestRelease() {
    return new Promise((resolve, reject) => {
        const options = { headers: { "User-Agent": "FoxCord-Installer" } };

        const makeRequest = (url) => {
            https.get(url, options, (res) => {
                if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
                    makeRequest(res.headers.location);
                    return;
                }
                if (res.statusCode !== 200) return reject(new Error(`API Error: ${res.statusCode}`));

                let data = "";
                res.on("data", (chunk) => data += chunk);
                res.on("end", () => {
                    try {
                        const json = JSON.parse(data);
                        if (json.tag_name) {
                            const cleanVersion = json.tag_name.replace(/^v/i, "");
                            const asset = Array.isArray(json.assets) ? json.assets.find(a => a.name === "fxcd.zip") : null;
                            const downloadUrl = asset ? asset.browser_download_url : `https://github.com/FoxCordClient/FoxCord/releases/download/${json.tag_name}/fxcd.zip`;
                            resolve({ version: cleanVersion, downloadUrl });
                        } else {
                            reject(new Error("tag_name not found"));
                        }
                    } catch (err) {
                        reject(err);
                    }
                });
            }).on("error", reject);
        };
        makeRequest(GITHUB_RELEASE_API);
    });
}

/**
 * Baixa o ZIP atualizando o Progresso na interface.
 */
function downloadZip(url, destPath, win, version) {
    return new Promise((resolve, reject) => {
        const file = fs.createWriteStream(destPath);
        const makeDownloadRequest = (targetUrl) => {
            https.get(targetUrl, { headers: { "User-Agent": "FoxCord-Installer" } }, (res) => {
                if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
                    makeDownloadRequest(res.headers.location);
                    return;
                }
                if (res.statusCode !== 200) return reject(new Error(`Status code: ${res.statusCode}`));

                const totalBytes = parseInt(res.headers["content-length"] || "0", 10);
                let downloadedBytes = 0;

                res.on("data", (chunk) => {
                    downloadedBytes += chunk.length;
                    file.write(chunk);
                    if (totalBytes > 0) {
                        const percent = Math.floor((downloadedBytes / totalBytes) * 100);
                        updateUI(win, `Downloading FoxCord v${version}...`, percent);
                    }
                });

                res.on("end", () => { file.end(); resolve(); });
                res.on("error", (err) => { file.end(); fs.unlink(destPath, () => {}); reject(err); });
            }).on("error", (err) => { file.end(); fs.unlink(destPath, () => {}); reject(err); });
        };
        makeDownloadRequest(url);
    });
}

/**
 * Garante que os arquivos extraídos fiquem na raiz da pasta de destino,
 * mesmo se o zip contiver uma pasta raiz interna (ex: app-1.2.1/FoxCord/...).
 */
function flattenSubdirectoryIfNeeded(targetDir) {
    if (!fs.existsSync(targetDir)) return;

    // Se já contém FoxCord.exe ou resources diretamente, a estrutura está correta
    if (fs.existsSync(path.join(targetDir, "FoxCord.exe")) || fs.existsSync(path.join(targetDir, "resources"))) {
        return;
    }

    const items = fs.readdirSync(targetDir, { withFileTypes: true });
    // Se extraiu em uma única subpasta
    if (items.length === 1 && items[0].isDirectory()) {
        const subDir = path.join(targetDir, items[0].name);
        const subItems = fs.readdirSync(subDir);
        for (const item of subItems) {
            const oldPath = path.join(subDir, item);
            const newPath = path.join(targetDir, item);
            fs.renameSync(oldPath, newPath);
        }
        fs.rmdirSync(subDir);
    }
}

/**
 * Extrai os arquivos do ZIP tratando erros de permissão e organizando pastas.
 */
function extractAndCleanOldVersions(zipPath, targetDir, latestVersion) {
    if (!fs.existsSync(targetDir)) fs.mkdirSync(targetDir, { recursive: true });

    let extractedSuccessfully = false;

    // Tentativa 1: AdmZip
    try {
        const zip = new AdmZip(zipPath);
        zip.extractAllTo(targetDir, true);
        extractedSuccessfully = true;
    } catch (err) {
        console.warn("Aviso: Falha ao extrair com AdmZip, tentando método nativo:", err.message);
    }

    // Tentativa 2: Fallback para PowerShell no Windows caso o AdmZip falhe com chmod
    if (!extractedSuccessfully && process.platform === "win32") {
        try {
            const powershellCmd = `Expand-Archive -Path "${zipPath}" -DestinationPath "${targetDir}" -Force`;
            execSync(`powershell -NoProfile -Command "${powershellCmd}"`, { stdio: "ignore" });
            extractedSuccessfully = true;
        } catch (err) {
            throw new Error(`Falha ao extrair arquivo ZIP: ${err.message}`);
        }
    }

    // Remove o ZIP baixado
    if (fs.existsSync(zipPath)) {
        try { fs.unlinkSync(zipPath); } catch (e) {}
    }

    // Corrige subpastas internas extras
    flattenSubdirectoryIfNeeded(targetDir);

    // Remove versões antigas em FOXCORD_DIR
    const currentAppName = `app-${latestVersion}`;
    if (fs.existsSync(FOXCORD_DIR)) {
        const entries = fs.readdirSync(FOXCORD_DIR, { withFileTypes: true });
        for (const entry of entries) {
            if (entry.isDirectory() && entry.name.startsWith("app-") && entry.name !== currentAppName) {
                const oldDirPath = path.join(FOXCORD_DIR, entry.name);
                try {
                    fs.rmSync(oldDirPath, { recursive: true, force: true, maxRetries: 3, retryDelay: 200 });
                } catch (err) {}
            }
        }
    }
}

/**
 * Lança o processo principal destacadamente e fecha o instalador.
 */
function launchFoxCord(targetAppDir) {
    const exePath = path.join(targetAppDir, "FoxCord.exe");
    if (fs.existsSync(exePath)) {
        const child = spawn(exePath, [], { detached: true, stdio: "ignore", cwd: path.dirname(exePath) });
        child.unref();
    }
    app.quit();
}

/**
 * ETAPAS DE INSTALAÇÃO PRINCIPAIS
 */
async function runUpdatePipeline(win) {
    try {
        // ETAPA 1: Preparação e Checagem (API)
        updateUI(win, "Checking internet connection...", 0);
        let latestVersion = "1.0.0";
        let targetDownloadUrl = ZIP_DOWNLOAD_URL;

        try {
            const releaseInfo = await fetchLatestRelease();
            latestVersion = releaseInfo.version;
            targetDownloadUrl = releaseInfo.downloadUrl;
        } catch (err) {
            console.warn("API fallback. Usando links padroes.");
        }

        const targetAppDir = path.join(FOXCORD_DIR, `app-${latestVersion}`);
        const tempBackupPasta = path.join(os.tmpdir(), "FoxCord.exe.WebView2.Backup");

        // ETAPA 2: Força o fechamento do App e Prepara Pastas
        updateUI(win, "Installing FoxCord...", 10);
        await killFoxCordProcess();
        await new Promise(r => setTimeout(r, 1000)); // Aguarda liberação de bloqueios do SO

        if (!fs.existsSync(FOXCORD_DIR)) fs.mkdirSync(FOXCORD_DIR, { recursive: true });

        updateUI(win, "Installing FoxCord...", 20);
        backupWebView2Data(FOXCORD_DIR, tempBackupPasta); // Backup WebView2

        // ETAPA 3: Download
        const zipPath = path.join(FOXCORD_DIR, "fxcd.zip");
        await downloadZip(targetDownloadUrl, zipPath, win, latestVersion);

        // ETAPA 4: Instalação / Extração
        updateUI(win, "Installing FoxCord...", 30);
        await new Promise(r => setTimeout(r, 500));
        extractAndCleanOldVersions(zipPath, targetAppDir, latestVersion);

        updateUI(win, "Installing FoxCord...", 70);
        restoreWebView2Data(tempBackupPasta, targetAppDir); // Restaura WebView2 na pasta nova

        updateUI(win, "Installing FoxCord...", 85);
        const exePath = path.join(targetAppDir, "FoxCord.exe");
        createShortcuts(exePath);
        createRegistryUninstaller(FOXCORD_DIR, exePath, latestVersion);

        updateUI(win, "Installing FoxCord...", 100);

        // ETAPA 5: Finalização e Animação
        for (let i = 0; i < 15; i++) {
            let dots = ".".repeat(i % 4);
            updateUI(win, `Initializing FoxCord${dots}`, -1);
            await new Promise(r => setTimeout(r, 200));
        }

        updateUI(win, "FoxCord has been successfully installed", -1);
        await new Promise(r => setTimeout(r, 1000));

        // Inicia e sai do instalador
        launchFoxCord(targetAppDir);

    } catch (error) {
        console.error("Erro na instalação:", error);
        updateUI(win, `Error: ${error.message}`, -1);
    }
}

function createWindow() {
    const win = new BrowserWindow({
        width: 300,
        height: 400,
        resizable: false,
        title: "FoxCord Installer",
        icon: path.join(__dirname, "app.ico"),
        autoHideMenuBar: true,
        titleBarStyle: "hidden",
        titleBarOverlay: {
            color: "#00000000",
            symbolColor: "#ffffff",
            height: 35
        },
        webPreferences: {
            preload: path.join(__dirname, "preload.js"),
            nodeIntegration: false,
            contextIsolation: true
        }
    });

    win.removeMenu();
    win.loadFile("index.html");

    win.webContents.on("did-finish-load", () => {
        runUpdatePipeline(win);
    });
}

app.whenReady().then(createWindow);

app.on("window-all-closed", () => {
    if (process.platform !== "darwin") {
        app.quit();
    }
});