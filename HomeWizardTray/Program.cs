using System;
using System.IO;
using System.Threading;
using GLib;
using HomeWizardTray.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using Log = Serilog.Log;

namespace HomeWizardTray;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "log.txt");
        Log.Logger = new LoggerConfiguration().WriteTo.File(logPath).CreateLogger();
        Log.Information("Starting app.");

        try
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, false)
                .Build();
            
            Log.Information(JsonConvert.SerializeObject(config));

            var host = Host
                .CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<AppSettings>();
                    services.AddDataProviders();
                    services.AddSingleton<App>();
                })
                .ConfigureAppConfiguration((ctx, builder) =>
                {
                    builder.AddUserSecrets<App>();
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