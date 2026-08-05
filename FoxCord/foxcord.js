const { app, BrowserWindow, session, desktopCapturer } = require("electron");
const path = require("path");
const applyFixButton = require("./functions/jscodes/fixhelpbutton");
const createSystemTray = require("./functions/system/systemtray");
const handleExternalLinks = require("./functions/system/openinbrowser"); // 1. Importação adicionada
const preventAnotherInstance = require("./functions/system/notanotherinstance");
const updatecheck = require("./functions/update");
let tray;

function createWindow() {
    const win = new BrowserWindow({
        width: 1200,
        height: 800,
        minWidth: 800,
        minHeight: 600,
        title: "FoxCord",
        icon: path.join(__dirname, "app.ico"),
        autoHideMenuBar: true,
        titleBarStyle: "hidden",
        titleBarOverlay: {
            color: "#00000000",
            symbolColor: "#ffffff",
            height: 30
        },
        webPreferences: {
            nodeIntegration: false,
            contextIsolation: true,
            sandbox: false,
            webSecurity: true,
            spellcheck: false
        }
    });

    win.removeMenu();
    applyFixButton(win);
    tray = createSystemTray(win);
    
    // 2. Chama a função para gerenciar os links externos
    handleExternalLinks(win);

    // Mantém o título sempre como FoxCord
    win.on("page-title-updated", (event) => {
        event.preventDefault();
        win.setTitle("FoxCord");
    });
    if (!preventAnotherInstance(createWindow)) {
        return;
    }

    win.loadURL("https://discord.com/app");
}

app.whenReady().then(async () => {
    const currentSession = session.defaultSession;
    const chromeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
    
    currentSession.setUserAgent(chromeUserAgent);

    currentSession.setPermissionRequestHandler((wc, permission, callback) => {
        const allowedPermissions = [
            "media",
            "display-capture",
            "fullscreen",
            "notifications",
            "pointerLock"
        ];
        
        if (allowedPermissions.includes(permission)) {
            return callback(true);
        }
        callback(false);
    });

    currentSession.setDisplayMediaRequestHandler((request, callback) => {
        desktopCapturer.getSources({ types: ["screen", "window"] }).then((sources) => {
            // Seleciona a primeira tela disponível por padrão
            if (sources.length > 0) {
                callback({ video: sources[0], audio: "loopback" });
            } else {
                callback({ video: true, audio: true });
            }
        }).catch((err) => {
            console.error("Erro ao capturar fontes de vídeo:", err);
            callback({ video: true, audio: true });
        });
    });

    currentSession.setPermissionCheckHandler((webContents, permission) => {
        if (permission === "media") return true;
        return true;
    });

    const ok = await updatecheck();

    if (!ok)
        return;

    createWindow();

    app.on("activate", () => {
        if (BrowserWindow.getAllWindows().length === 0)
            createWindow();
    });
});

app.on("window-all-closed", () => {
    if (process.platform === "darwin")
        app.quit();
});