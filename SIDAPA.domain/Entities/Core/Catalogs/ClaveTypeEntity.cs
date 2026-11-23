using SIDAPA.domain.Enums.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIDAPA.domain.Entities.Core.Catalogs
{
    public class ClaveTypeEntity
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Grupo { get; set; } = string.Empty;
        public string? SubGrupo { get; set; }
        public string? Supuestos { get; set; }
        public bool Vigente { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}
