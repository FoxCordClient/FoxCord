const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("electronAPI", {
    /**
     * Receives update status messages and percentage progress from main process
     * @param {Function} callback - Callback function receiving (event, data)
     */
    onUpdateStatus: (callback) => {
        ipcRenderer.on("update-status", (event, data) => callback(event, data));
    }
});