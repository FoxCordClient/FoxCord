const { app, dialog } = require("electron");
const fs = require("fs");
const path = require("path");
const https = require("https");
const { spawn } = require("child_process");
const packageJson = require("../package.json");

const API = "https://api.github.com/repos/FoxCordClient/FoxCord/releases/latest";

// Usa o LocalAppData do usuário atual dinamicamente em vez de um caminho fixo
const DOWNLOAD_FOLDER = path.join(
    process.env.LOCALAPPDATA || app.getPath("userData"),
    "FoxCord"
);

const DOWNLOAD_FILE = path.join(DOWNLOAD_FOLDER, "installer.exe");

function download(url) {
    return new Promise((resolve, reject) => {
        // Garante que a pasta de destino exista
        fs.mkdirSync(DOWNLOAD_FOLDER, { recursive: true });

        https.get(url, {
            headers: {
                "User-Agent": "FoxCord"
            }
        }, (res) => {
            // Lida com redirecionamentos HTTP do GitHub (301, 302, etc.)
            if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
                return download(res.headers.location)
                    .then(resolve)
                    .catch(reject);
            }

            if (res.statusCode !== 200) {
                return reject(new Error(`Download failed: HTTP ${res.statusCode}`));
            }

            const file = fs.createWriteStream(DOWNLOAD_FILE);

            res.pipe(file);

            file.on("finish", () => {
                file.close(() => resolve(DOWNLOAD_FILE));
            });

            file.on("error", (err) => {
                fs.unlink(DOWNLOAD_FILE, () => {}); // Remove arquivo incompleto em caso de erro
                reject(err);
            });

        }).on("error", (err) => {
            reject(err);
        });
    });
}

module.exports = async function () {
    try {
        const release = await new Promise((resolve, reject) => {
            https.get(API, {
                headers: {
                    "User-Agent": "FoxCord"
                }
            }, (res) => {
                let body = "";

                if (res.statusCode !== 200) {
                    return reject(new Error(`GitHub API HTTP ${res.statusCode}`));
                }

                res.on("data", d => body += d);

                res.on("end", () => {
                    try {
                        resolve(JSON.parse(body));
                    } catch (err) {
                        reject(err);
                    }
                });

            }).on("error", reject);
        });

        const latest = release.tag_name.replace(/^v/i, "");
        const current = packageJson.version.replace(/^v/i, "");

        // Se estiver na versão mais recente, continua o aplicativo
        if (latest === current) {
            return true;
        }

        const asset = release.assets ? release.assets.find(a => a.name === "installer.exe") : null;

        if (!asset) {
            dialog.showErrorBox(
                "Update Error",
                "The installer could not be found in the latest release."
            );
            app.quit();
            return false;
        }

        await dialog.showMessageBox({
            type: "warning",
            buttons: ["OK"],
            defaultId: 0,
            cancelId: 0,
            title: "Update Required",
            message: "Your version of FoxCord is outdated.\n\nTo continue using FoxCord you must download the latest version.\n\nThe latest installer will now be downloaded and launched."
        });

        await download(asset.browser_download_url);

        // Spawna o CMD ocultado para executar o instalador de forma totalmente desvinculada
        const child = spawn("cmd.exe", ["/c", "start", "", DOWNLOAD_FILE], {
            detached: true,
            stdio: "ignore",
            windowsHide: true
        });

        // unref() remove o processo filho do Event Loop principal do Node/Electron,
        // permitindo que o aplicativo fecha sem esperar pelo instalador.
        child.unref();

        app.quit();

        return false;

    } catch (err) {
        console.error("Update error:", err);

        dialog.showErrorBox(
            "Update Error",
            "Unable to check or apply updates."
        );

        app.quit();

        return false;
    }
};