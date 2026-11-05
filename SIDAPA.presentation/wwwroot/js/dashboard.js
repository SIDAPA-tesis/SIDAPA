(() => {
    const root = document.documentElement;
    const toggle = document.getElementById("toggleSidebar");
    const movil = window.matchMedia("(max-width: 768px)");

    if (!toggle) return;

    const sincronizar = () => {
        const contraido = root.classList.contains("sidebar-collapsed");
        toggle.setAttribute("aria-expanded", String(!contraido));
    };

    toggle.addEventListener("click", () => {
        const contraido = root.classList.toggle("sidebar-collapsed");

        if (!movil.matches) {
            try {
                localStorage.setItem("sidebar-collapsed", contraido ? "1" : "0");
            } catch (e) { }
        }

        sincronizar();
    });

    sincronizar();
})();
