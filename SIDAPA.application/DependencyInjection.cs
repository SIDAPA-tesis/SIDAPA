using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIDAPA.application.Interface.CaseFile.Documents;
using SIDAPA.application.Interface.CaseFile.Expediente;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Services.CaseFile.Documents;
using SIDAPA.application.Services.CaseFile.Expediente;
using SIDAPA.application.Services.Core;

namespace SIDAPA.application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.TryAddSingleton(TimeProvider.System);

            services.AddScoped<IPedimentoService, PedimentoService>();
            services.AddScoped<IClaveService, ClaveService>();
            services.AddScoped<IArrivalNoticeService, ArrivalNoticeService>();   // SCRUM-46
            services.AddScoped<IDocumentRequestService, DocumentRequestService>();   // SCRUM-64 (HU-05)
            services.AddScoped<IExpedienteReviewService, ExpedienteReviewService>();   // SCRUM-55 (HU-03)
            services.AddSingleton<IPermisosService, PermisosService>();

            return services;
        }
    }
}
