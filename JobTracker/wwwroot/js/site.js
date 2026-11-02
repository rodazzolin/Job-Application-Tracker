// Light/dark mode toggle. Bootstrap themes everything from the data-bs-theme attribute on <html>;
// the choice is saved in localStorage so it carries across pages (the inline script in
// _Layout.cshtml applies it on load).
(() => {
    const toggle = document.getElementById("theme-toggle");
    if (!toggle) return;

    toggle.addEventListener("click", () => {
        const root = document.documentElement;
        const next = root.dataset.bsTheme === "light" ? "dark" : "light";
        root.dataset.bsTheme = next;
        try {
            localStorage.setItem("theme", next);
        } catch { /* Storage blocked: the theme still changes for this page. */ }
    });
})();
