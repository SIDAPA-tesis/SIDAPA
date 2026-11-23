using System;
using System.Collections.Generic;
using System.Text;

namespace SIDAPA.application.Interface.Core
{
    public interface IUserActual
    {
        string Nombre { get; }
        IReadOnlyCollection<string> Roles { get; }
    }
}
