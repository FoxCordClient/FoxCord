const { app, BrowserWindow } = require("electron");

function preventAnotherInstance(createWindow) {
    const gotTheLock = app.requestSingleInstanceLock();

    if (!gotTheLock) {
        app.quit();
        return false;
    }

    app.on("second-instance", () => {
        const windows = BrowserWindow.getAllWindows();

        if (windows.length > 0) {
            const win = windows[0];

            if (win.isMinimized()) {
                win.restore();
            }

            win.show();
            win.focus();
        } else if (typeof createWindow === "function") {
            createWindow();
        }
    });

    return true;
}

module.exports = preventAnotherInstance;