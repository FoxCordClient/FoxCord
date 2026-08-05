module.exports = function applyFixButton(win) {
    win.webContents.on("dom-ready", () => {
        win.webContents.insertCSS(`
            /* Permite arrastar a janela pelas áreas neutras do cabeçalho */
            header[class*="upperContainer_"],
            [class*="typeWindows_"],
            [class*="title_"] {
                -webkit-app-region: drag;
            }

            /* Garante que os botões permaneçam clicáveis */
            button,
            a,
            input,
            textarea,
            svg,
            [role="button"],
            [class*="clickable_"],
            [class*="iconWrapper_"],
            [class*="toolbar_"] *,
            [class*="trailing_"] * {
                -webkit-app-region: no-drag !important;
                pointer-events: auto !important;
            }

            /* Afasta os ícones do Discord dos botões nativos */
            [class*="trailing_"],
            [class*="toolbar_"],
            header [class*="upperContainer_"] > div:last-child {
                margin-right: 140px !important;
                padding-right: 10px !important;
                transition: margin-right 0.2s ease;
            }

            /* Ajuste de segurança */
            [class*="typeWindows_"] {
                padding-right: 140px !important;
            }
        `);
    });
};