using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SIDAPA.presentation.Models
{
    public record OpcionMenuViewModel(string Texto, string Controlador, bool Activa);

    public record MenuLateralViewModel(string Nombre, string Rol, IReadOnlyList<OpcionMenuViewModel> Opciones);
}
