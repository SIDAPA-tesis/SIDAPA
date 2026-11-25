// Aviso de arribo (Views/Avisos/Nuevo.cshtml). Mejora progresiva: sin este archivo el formulario funciona igual
// con los renglones de archivo y un POST normal. Aquí solo se agrega:
//  - una zona para arrastrar y soltar que acomoda cada archivo en el primer renglón libre;
//  - la revisión de extensión y tamaño en el navegador, para avisar antes de enviar y no perder los demás archivos.
// La validación que cuenta es la del servidor (DocumentFileRule).
(function () {
    "use strict";

    var form = document.querySelector("[data-avisos-form]");
    if (!form) return;

    var maxBytes = Number(form.dataset.maxBytes);
    var extensiones = (form.dataset.extensiones || "").split(",");
    var zona = form.querySelector("[data-avisos-dropzone]");
    var error = form.querySelector("[data-avisos-error]");
    var entradas = Array.prototype.slice.call(form.querySelectorAll("[data-avisos-archivo]"));

    function extension(nombre) {
        var punto = nombre.lastIndexOf(".");
        return punto < 0 ? "" : nombre.slice(punto).toLowerCase();
    }

    // Regresa el mensaje de error del archivo, o null si se puede enviar.
    function revisar(archivo) {
        if (extensiones.indexOf(extension(archivo.name)) < 0)
            return "\"" + archivo.name + "\" no es de un tipo permitido. Solo PDF, XML o Excel (.xls, .xlsx).";
        if (archivo.size > maxBytes)
            return "\"" + archivo.name + "\" pesa más de " + Math.round(maxBytes / 1048576) + " MB.";
        if (archivo.size === 0)
            return "\"" + archivo.name + "\" está vacío.";
        return null;
    }

    function marcar(entrada) {
        var renglon = entrada.closest("[data-avisos-renglon]");
        if (renglon) renglon.classList.toggle("avisos-renglon--lleno", entrada.files.length > 0);
    }

    function mostrar(mensajes) {
        error.textContent = mensajes.join(" ");
    }

    // Al elegir un archivo en un renglón: si no pasa la revisión se quita y se avisa.
    entradas.forEach(function (entrada) {
        entrada.addEventListener("change", function () {
            var archivo = entrada.files[0];
            var mensaje = archivo ? revisar(archivo) : null;
            if (mensaje) entrada.value = "";
            mostrar(mensaje ? [mensaje] : []);
            marcar(entrada);
        });
    });

    // Arrastrar y soltar solo si el navegador permite asignar archivos a un <input type="file">.
    if (!zona || typeof DataTransfer === "undefined") return;
    zona.hidden = false;

    ["dragenter", "dragover"].forEach(function (evento) {
        zona.addEventListener(evento, function (e) {
            e.preventDefault();
            zona.classList.add("avisos-dropzone--activa");
        });
    });

    ["dragleave", "drop"].forEach(function (evento) {
        zona.addEventListener(evento, function () {
            zona.classList.remove("avisos-dropzone--activa");
        });
    });

    zona.addEventListener("drop", function (e) {
        e.preventDefault();
        var mensajes = [];

        Array.prototype.forEach.call(e.dataTransfer.files, function (archivo) {
            var mensaje = revisar(archivo);
            if (mensaje) {
                mensajes.push(mensaje);
                return;
            }

            var libre = entradas.find(function (entrada) { return entrada.files.length === 0; });
            if (!libre) {
                mensajes.push("Ya no hay renglones libres para \"" + archivo.name + "\".");
                return;
            }

            var transferencia = new DataTransfer();
            transferencia.items.add(archivo);
            libre.files = transferencia.files;
            marcar(libre);
        });

        mostrar(mensajes);
    });
})();
