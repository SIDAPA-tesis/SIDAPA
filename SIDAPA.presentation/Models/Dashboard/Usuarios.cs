using Microsoft.AspNetCore.Mvc;

namespace SIDAPA.presentation.Models.Dashboard
{
    public record Usuario(int Id, string Nombre, string Correo, string Rol, bool Activo);
}
