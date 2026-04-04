using System;
using System.IO;
using System.Reflection;
using Avalonia;
using HomeWizardTray.DataProviders;
using HomeWizardTray.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace HomeWizardTray;

internal static class Program
{
    public static string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
    public static IHost AppHost { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "log.txt");
        Log.Logger = new LoggerConfiguration().WriteTo.File(logPath).CreateLogger();

        try
        {
            AppHost = Host
                .CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<AppSettings>();
                    services.AddSingleton<NotificationService>();
                    services.AddDataProviders();
                })
                .ConfigureAppConfiguration((_, builder) => { builder.AddUserSecrets<App>(); })
                .Build();

            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                AppHost.Dispose();
                Log.CloseAndFlush();
            };

            var logger = AppHost.Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Starting app version {Version}.", Version);

            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while starting the app.");
            throw;
        }
    }
}