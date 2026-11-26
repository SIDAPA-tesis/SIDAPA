using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.Services.Core
{
    /// <summary>
    /// Única fuente de "qué módulos ve cada rol". La usan el menú lateral y las tarjetas de accesos rápidos.
    /// Debe coincidir con las políticas de cada controller (presentation/Security/AuthorizationSetup.cs y
    /// docs/permisos-por-rol.md): el menú solo esconde opciones, quien protege es el servidor.
    /// </summary>
    public class PermisosService : IPermisosService
    {
        // Ordinal: el rol del token se compara exacto, igual que en las políticas ("glosador" no es GLOSADOR).
        private static readonly Dictionary<string, ModuleEnum[]> PorRol = new(StringComparer.Ordinal)
        {
            [nameof(RolesEnum.CAPTURADOR)] = [ModuleEnum.Pedimentos],                                           // Staff
            [nameof(RolesEnum.GLOSADOR)] = [ModuleEnum.Pedimentos, ModuleEnum.Claves, ModuleEnum.Productos],   // Staff + Glosa
            [nameof(RolesEnum.ADMINISTRADOR)] = Enum.GetValues<ModuleEnum>().Where(m => m != ModuleEnum.AvisosArribo).ToArray(),
            [nameof(RolesEnum.VALIDADOR)] = [],   // sin política todavía (docs/permisos-por-rol.md): solo Inicio
            [nameof(RolesEnum.CLIENTE)] = [ModuleEnum.AvisosArribo],   // solo su portal (ClientPortal)
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
