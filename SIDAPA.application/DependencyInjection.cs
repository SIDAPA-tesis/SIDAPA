using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Services.Core;
using SIDAPA.application.DTOs.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIDAPA.application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.TryAddSingleton(TimeProvider.System);

            services.AddScoped<IPedimentoService, PedimentoService>();
            services.AddScoped<IClaveService, ClaveService>();
            services.AddSingleton<IPermisosService, PermisosService>();

            return services;
        }
    }
}
