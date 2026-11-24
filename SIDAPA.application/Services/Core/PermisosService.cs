using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Enums.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace SIDAPA.application.Services.Core
{
    public class PermisosService : IPermisosService
    {
        // Qué módulos ve cada rol. Cambia aquí cuando cambien las responsabilidades.
        private static readonly Dictionary<string, ModuleEnum[]> PorRol = new(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(RolesEnum.CAPTURADOR)] = [ModuleEnum.Pedimentos],
            [nameof(RolesEnum.GLOSADOR)] = [ModuleEnum.Pedimentos, ModuleEnum.Claves, ModuleEnum.Reportes],
            [nameof(RolesEnum.ADMINISTRADOR)] = Enum.GetValues<ModuleEnum>().Where(m => m != ModuleEnum.AvisosArribo).ToArray(),
            [nameof(RolesEnum.VALIDADOR)] = [],   // sin política todavía (docs/permisos-por-rol.md): solo Inicio
            [nameof(RolesEnum.CLIENTE)] = [ModuleEnum.AvisosArribo],   // solo su portal (docs/permisos-por-rol.md)
        };

        public IReadOnlyList<ModuleEnum> ModulosPara(IEnumerable<string> roles) =>
            roles.Where(PorRol.ContainsKey)
                 .SelectMany(r => PorRol[r])
                 .Append(ModuleEnum.Inicio)
                 .Distinct()
                 .Order()
                 .ToList();
    }
}
