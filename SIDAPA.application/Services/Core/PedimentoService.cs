using SIDAPA.application.DTOs.Core.Pedimento;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Catalogs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIDAPA.application.Services.Core
{
    public class PedimentoService : IPedimentoService
    {
        private readonly IPedimentoRepositorio _repositorio;
        private readonly TimeProvider _reloj;

        public PedimentoService(IPedimentoRepositorio repositorio, TimeProvider reloj)
        {
            _repositorio = repositorio;
            _reloj = reloj;
        }

        public async Task<PanelPedimentosDto> ObtenerPanelAsync(string? busqueda, DateOnly? mes, CancellationToken ct = default)
        {
            var hoy = Hoy();
            var referencia = mes ?? hoy;
            var primerDia = new DateOnly(referencia.Year, referencia.Month, 1);

            var pedimentos = await _repositorio.ListarAsync(busqueda, ct);

            return new PanelPedimentosDto(pedimentos.Select(p => Mapear(p, hoy)).ToList(), primerDia, hoy);
        }

        public async Task<PedimentoDto?> ObtenerAsync(int id, CancellationToken ct = default)
        {
            var pedimento = await _repositorio.ObtenerAsync(id, ct);
            return pedimento is null ? null : Mapear(pedimento, Hoy());
        }

        private DateOnly Hoy() => DateOnly.FromDateTime(_reloj.GetLocalNow().DateTime);

        private static PedimentoDto Mapear(PedimentoTypeEntity p, DateOnly hoy) =>
            new(p.Id, p.Numero, p.Empresa, p.ImagenUrl, p.Avance, p.FechaLimite, p.UrgenciaEn(hoy));
    }
}
