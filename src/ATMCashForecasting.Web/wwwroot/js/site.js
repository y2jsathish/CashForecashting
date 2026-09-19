(function () {
    "use strict";

    const root = document.documentElement;
    const themeToggle = document.getElementById("themeToggle");
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("sidebar");
    const THEME_KEY = "acf-theme";

    function applyTheme(theme) {
        root.setAttribute("data-bs-theme", theme);
        if (themeToggle) {
            const icon = themeToggle.querySelector("i");
            if (icon) {
                icon.className = theme === "dark" ? "bi bi-sun" : "bi bi-moon-stars";
            }
        }
    }

    try {
        const saved = localStorage.getItem(THEME_KEY);
        if (saved) applyTheme(saved);
    } catch (e) { /* localStorage unavailable; default theme stands */ }

    themeToggle?.addEventListener("click", function () {
        const next = root.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";
        applyTheme(next);
        try { localStorage.setItem(THEME_KEY, next); } catch (e) { /* ignore */ }
    });

    sidebarToggle?.addEventListener("click", function () {
        sidebar?.classList.toggle("collapsed");
        sidebar?.classList.toggle("show");
    });

    // Highlights the sidebar link matching the current controller.
    document.querySelectorAll(".sidebar .nav-link").forEach(function (link) {
        if (link.getAttribute("href") === window.location.pathname) {
            link.classList.add("active");
        }
    });
})();

/** Shared helper: attaches the JWT bearer token (obtained at login) to fetch() calls against /api/*. */
const AtmCashApi = {
    async call(url, options = {}) {
        const token = sessionStorage.getItem("acf-jwt");
        const headers = Object.assign({}, options.headers, token ? { Authorization: `Bearer ${token}` } : {});
        const response = await fetch(url, Object.assign({}, options, { headers }));
        if (!response.ok) {
            throw new Error(`Request to ${url} failed with status ${response.status}`);
        }
        return response.status === 204 ? null : response.json();
    }
};
