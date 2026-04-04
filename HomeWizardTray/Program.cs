using System;
using System.IO;
using System.Reflection;
using HomeWizardTray.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace HomeWizardTray;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "log.txt");
        Log.Logger = new LoggerConfiguration().WriteTo.File(logPath).CreateLogger();

        try
        {
            var host = Host
                .CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<AppSettings>();
                    services.AddSingleton<CommandQueue>();
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
                Log.CloseAndFlush();
            };

            var logger = host.Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Starting app (v{Version})", Assembly.GetExecutingAssembly().GetName().Version);

            host.Services.GetRequiredService<App>().Start();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while starting the app.");
            throw;
        }
    }
}