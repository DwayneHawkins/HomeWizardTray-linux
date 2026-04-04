using System.Net.Http;
using HomeWizardTray.DataProviders.Daikin;
using HomeWizardTray.DataProviders.HomeWizard;
using HomeWizardTray.DataProviders.Sma;
using Microsoft.Extensions.DependencyInjection;

namespace HomeWizardTray.DataProviders;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataProviders(this IServiceCollection services)
    {
        services.AddHttpClient<DaikinFtxm25DataProvider>();

        services.AddHttpClient<HomeWizardP1DataProvider>();

        services
            .AddHttpClient<SmaSunnyBoyDataProvider>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // Accept broken or lacking certificate from SMA Sunny Boy
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        
        return services;
    }
}