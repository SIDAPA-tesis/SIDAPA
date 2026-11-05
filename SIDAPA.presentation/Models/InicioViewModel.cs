using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SIDAPA.presentation.Models
{
    public record Pendiente(string Titulo, string Vence);

    public record InicioViewModel(string Saludo, IReadOnlyList<Pendiente> Pendientes);
}
