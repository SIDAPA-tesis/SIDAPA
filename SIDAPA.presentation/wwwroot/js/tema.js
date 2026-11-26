// Tema claro/oscuro de Bootstrap (data-bs-theme) para la portada (_inicioLayout).
// El tema guardado ya lo aplicó el script en línea del <head>; aquí solo se conecta el botón "Cambiar tema".
(() => {
    const boton = document.getElementById("cambiarTema");
    if (!boton) return;

    boton.addEventListener("click", () => {
        const html = document.documentElement;
        const nuevo = html.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";
        html.setAttribute("data-bs-theme", nuevo);

        try {
            localStorage.setItem("tema", nuevo);
        } catch (e) { }
    });
})();
