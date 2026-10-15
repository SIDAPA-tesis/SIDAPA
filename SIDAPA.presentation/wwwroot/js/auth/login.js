// Inicio de sesión con fetch. Sin JavaScript el formulario hace un post normal y el servidor
// vuelve a pintar la vista con el error, así que todo aquí es una mejora progresiva.
(() => {
    const form = document.getElementById("loginForm");
    const stage = document.getElementById("loginStage");
    const errorBox = document.getElementById("loginError");
    if (!form || !stage || !errorBox || !window.fetch) return;

    const ANIMATION_MS = 600; // igual que --login-anim-ms en login.css
    const GENERIC_ERROR = "No se pudo iniciar sesión. Intente de nuevo.";
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    const buttons = form.querySelectorAll("button");
    const spinner = form.querySelector("[data-login-spinner]");
    const password = form.querySelector("input[type=password]");

    // Botones con type="button" y data-login-action="<nombre>" ejecutan loginActions[nombre].
    // Hoy el botón circular es type="submit" y por eso envía el formulario.
    const loginActions = {};

    form.addEventListener("click", (event) => {
        const button = event.target.closest("button[type=button][data-login-action]");
        if (button) loginActions[button.dataset.loginAction]?.(event);
    });

    let busy = false;

    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        if (busy) return;
        setBusy(true);
        hideError();

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: { Accept: "application/json" },
                credentials: "same-origin"
            });
            // 400 = el formulario ya no es válido (otra sesión abierta o la app se reinició): se recarga la página.
            if (response.status === 400) {
                showError("La página expiró. Recargando…");
                window.location.reload();
                return;
            }
            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const result = await response.json();
            if (result.ok) {
                await playExit();
                window.location.assign(result.redirectUrl);
                return; // se queda "ocupado" hasta que cambie la página
            }
            showError(result.error || GENERIC_ERROR);
        } catch {
            showError(GENERIC_ERROR);
        }

        password.value = "";
        password.focus();
        setBusy(false);
    });

    function playExit() {
        if (reducedMotion.matches) return Promise.resolve();
        stage.classList.add("is-leaving");
        return new Promise((resolve) => setTimeout(resolve, ANIMATION_MS));
    }

    function setBusy(value) {
        busy = value;
        buttons.forEach((b) => (b.disabled = value));
        spinner?.classList.toggle("d-none", !value);
        form.setAttribute("aria-busy", String(value));
    }

    function showError(message) {
        errorBox.textContent = message;
        errorBox.classList.remove("d-none");
    }

    function hideError() {
        errorBox.textContent = "";
        errorBox.classList.add("d-none");
    }
})();
