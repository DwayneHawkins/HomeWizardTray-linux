using System;
using System.Net.Http;
using HomeWizardTray.DataProviders.Daikin;
using HomeWizardTray.DataProviders.HomeWizard;
using HomeWizardTray.DataProviders.Sma;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HomeWizardTray;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.File("log.txt").CreateLogger();
        Log.Information("Starting app.");

        try
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", false).Build();
            
            var host = Host
                .CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<App>();
                    services.AddSingleton<AppSettings>();
                    services.AddHttpClient<DaikinFtxm25DataProvider>();
                    services.AddHttpClient<HomeWizardP1DataProvider>();

                    services
                        .AddHttpClient<SmaSunnyBoyDataProvider>()
                        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                        {
                            // Accept broken or lacking certificate from SMA Sunny Boy
                            // TODO move and make it specific to the sunnyboy data provider class
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        });
                })
                .Build();

            AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.Information("Exited app.");
            host.Services.GetRequiredService<App>().Start();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while starting the app.");
            throw;
        }
    }
}