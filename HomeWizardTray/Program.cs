using System;
using HomeWizardTray.Util;
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
                    services.AddSingleton<AppSettings>();
                    services.AddDataProviders();
                    services.AddTransient<App>();
                })
                .Build();

            AppDomain.CurrentDomain.ProcessExit += (_, _) => 
            {
                host.Dispose();
                Log.Information("Exited app.");
            };
            
            host.Services.GetRequiredService<App>().Start();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while starting the app.");
            throw;
        }
    }
}