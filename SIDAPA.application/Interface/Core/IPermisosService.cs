using SIDAPA.domain.Enums.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SIDAPA.application.Interface.Core
{
    public interface IPermisosService
    {
        /// <summary>Módulos a los que tienen acceso los roles indicados, en el orden del menú.</summary>
        IReadOnlyList<ModuleEnum> ModulosPara(IEnumerable<string> roles);
    }
}
