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
            ["CAPTURADOR"] = [ModuleEnum.Pedimentos],
            ["GLOSADOR"] = [ModuleEnum.Pedimentos, ModuleEnum.Claves, ModuleEnum.Reportes],
            ["ADMINISTRADOR"] = Enum.GetValues<ModuleEnum>(),
            ["VALIDADOR"] = [ModuleEnum.Pedimentos],
            ["CLIENTE"] = Enum.GetValues<ModuleEnum>(),
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
