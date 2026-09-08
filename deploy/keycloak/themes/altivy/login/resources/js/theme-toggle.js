(function () {
    "use strict";

    var STORAGE_KEY = "altivy-theme";
    var root = document.documentElement;

    function readStoredTheme() {
        try {
            return window.localStorage.getItem(STORAGE_KEY);
        } catch (error) {
            return null;
        }
    }

    function persistTheme(theme) {
        try {
            window.localStorage.setItem(STORAGE_KEY, theme);
        } catch (error) {
            // Web Storage indisponível (ex.: modo privado); a escolha vale só para esta página.
        }
    }

    function applyTheme(theme) {
        if (theme === "light" || theme === "dark") {
            root.setAttribute("data-altivy-theme", theme);
        } else {
            root.removeAttribute("data-altivy-theme");
        }
    }

    function effectiveTheme() {
        var stored = readStoredTheme();
        if (stored === "light" || stored === "dark") {
            return stored;
        }
        var prefersDark = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
        return prefersDark ? "dark" : "light";
    }

    function updateToggle(button, theme) {
        var nextThemeLabel = theme === "dark" ? "claro" : "escuro";
        button.textContent = theme === "dark" ? "Tema claro" : "Tema escuro";
        button.setAttribute("aria-label", "Ativar tema " + nextThemeLabel);
        button.setAttribute("aria-pressed", theme === "dark" ? "true" : "false");
    }

    function createToggle() {
        var button = document.createElement("button");
        button.type = "button";
        button.className = "altivy-theme-toggle";
        updateToggle(button, effectiveTheme());

        button.addEventListener("click", function () {
            var next = effectiveTheme() === "dark" ? "light" : "dark";
            applyTheme(next);
            persistTheme(next);
            updateToggle(button, next);
        });

        return button;
    }

    function insertToggle() {
        var container = document.querySelector(".pf-v5-c-login__container");
        var header = document.getElementById("kc-header");
        var anchor = header || container;
        if (!anchor || !anchor.parentNode) {
            return;
        }
        anchor.parentNode.insertBefore(createToggle(), anchor);
    }

    // Aplica a preferência salva o quanto antes para reduzir o flash do tema padrão.
    var stored = readStoredTheme();
    if (stored === "light" || stored === "dark") {
        applyTheme(stored);
    }

    // O script é injetado perto do fim do body: o DOM pode já estar pronto e
    // "DOMContentLoaded" pode já ter disparado antes deste listener existir.
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", insertToggle);
    } else {
        insertToggle();
    }
})();
