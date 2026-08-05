const { shell } = require("electron");

/**
 * Intercepta a abertura de novos links e os redireciona para o navegador padrão.
 * @param {Electron.BrowserWindow} win - A instância da janela principal.
 */
function handleExternalLinks(win) {
    win.webContents.setWindowOpenHandler(({ url }) => {
        // Verifica se é uma URL externa válida (http ou https)
        if (url.startsWith("http:") || url.startsWith("https:")) {
            shell.openExternal(url);
            return { action: "deny" }; // Impede o Electron de abrir uma nova janela
        }
        return { action: "allow" };
    });
}

module.exports = handleExternalLinks;