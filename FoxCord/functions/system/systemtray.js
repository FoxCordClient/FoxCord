const { Tray, Menu, nativeImage, app } = require("electron");
const path = require("path");

module.exports = function createSystemTray(win) {
    const iconPath = path.join(__dirname, "../../app.ico");

    const tray = new Tray(iconPath);
    tray.setToolTip("FoxCord");

    //  Criamos a imagem e redimensionamos automaticamente (ex: 16x16 pixels)
    const rawIcon = nativeImage.createFromPath(iconPath);
    const appIcon = rawIcon.resize({ width: 16, height: 16 });

    const menu = Menu.buildFromTemplate([
        {
            label: "FoxCord",
            icon: appIcon, // Agora a imagem estará no tamanho correto!
            enabled: false
        },
        { type: "separator" },
        {
            label: "Open FoxCord",
            click() {
                if (win.isMinimized()) {
                    win.restore();
                }

                win.show();
                win.focus();
            }
        },
        { type: "separator" },
        {
            label: "Quit",
            click() {
                app.isQuiting = true;
                app.quit();
            }
        }
    ]);

    tray.setContextMenu(menu);

    // Left click always opens FoxCord
    tray.on("click", () => {
        if (win.isMinimized()) {
            win.restore();
        }

        win.show();
        win.focus();
    });

    // Hide instead of closing
    win.on("close", (e) => {
        if (!app.isQuiting) {
            e.preventDefault();
            win.hide();
        }
    });

    return tray;
};