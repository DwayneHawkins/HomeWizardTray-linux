using System;
using Serilog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using HomeWizardTray.DataProviders.Sma;
using HomeWizardTray.DataProviders.HomeWizard;
using HomeWizardTray.DataProviders.Daikin;

namespace HomeWizardTray;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.File("log.txt").CreateLogger();

        try
        {
            Log.Information("Building app config.");
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", false).Build();

            Log.Information("Building app host and DI container.");
            var host = Host
                .CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
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

                    services.AddTransient<App>();
                })
                .ConfigureAppConfiguration((ctx, builder) =>
                {
                    Log.Information("App running in {environment} environment.", ctx.HostingEnvironment.EnvironmentName);

                    /*if (ctx.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddUserSecrets<App>();
                    }*/
                })
                .Build();

            Log.Information("Starting app.");
            Gtk.Application.Init();
            var app = host.Services.GetRequiredService<App>();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => { Log.Information("Exiting app."); };
            Gtk.Application.Run();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while starting the app.");
            //MessageBox.Show("An error has occured. Please see log file.", "SunnyTray", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}