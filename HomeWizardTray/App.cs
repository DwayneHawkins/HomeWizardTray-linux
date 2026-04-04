using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using HomeWizardTray.DataProviders;
using HomeWizardTray.DataProviders.Daikin;
using HomeWizardTray.DataProviders.Daikin.Constants;
using HomeWizardTray.DataProviders.HomeWizard;
using HomeWizardTray.DataProviders.Sma;
using HomeWizardTray.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HomeWizardTray;

internal sealed class App : Application
{
    private HomeWizardP1DataProvider _homeWizardP1DataProvider;
    private SmaSunnyBoyDataProvider _smaSunnyBoyDataProvider;
    private DaikinFtxm25DataProvider _daikinFtxm25DataProvider;
    private NotificationService _notificationService;
    private ILogger<App> _logger;

    public override void OnFrameworkInitializationCompleted()
    {
        var services = Program.AppHost.Services;
        _homeWizardP1DataProvider = services.GetRequiredService<HomeWizardP1DataProvider>();
        _smaSunnyBoyDataProvider = services.GetRequiredService<SmaSunnyBoyDataProvider>();
        _daikinFtxm25DataProvider = services.GetRequiredService<DaikinFtxm25DataProvider>();
        _notificationService = services.GetRequiredService<NotificationService>();
        _logger = services.GetRequiredService<ILogger<App>>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var iconPath = Path.Combine(AppContext.BaseDirectory, "sun.png");
            var trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(new Bitmap(iconPath)),
                ToolTipText = $"HomeWizardTray v{Program.Version}",
                IsVisible = true,
                Menu = BuildMenu(desktop)
            };
            trayIcon.Clicked += async (_, _) => await SmaShowStatus();
            TrayIcon.SetIcons(this, [trayIcon]);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private NativeMenu BuildMenu(IClassicDesktopStyleApplicationLifetime desktop)
    {
        return AvaloniaMenuBuilder.Build(
        [
            new("DAIKIN"),
            new("Mode",
            [
                new("Normal", () => HandleProviderResult(_daikinFtxm25DataProvider.SetNormal())),
                new("Max", () => HandleProviderResult(_daikinFtxm25DataProvider.SetMax())),
                new("Eco", () => HandleProviderResult(_daikinFtxm25DataProvider.SetEco())),
                new("Dehumidify", () => HandleProviderResult(_daikinFtxm25DataProvider.SetDehumidify())),
                new("Off", () => HandleProviderResult(_daikinFtxm25DataProvider.SetOff()))
            ]),
            new("Status", DaikinShowStatus),
            new("-"),
            new("SUNNY BOY"),
            new("Status", SmaShowStatus),
            new("-"),
            new("v" + Program.Version),
            new("Logs", () =>
            {
                ShowLogs();
                return Task.CompletedTask;
            }),
            new("Quit", () =>
            {
                desktop.Shutdown();
                return Task.CompletedTask;
            })
        ]);
    }

    private async Task HandleProviderResult(Task<ProviderResult> task)
    {
        var result = await task;
        if (!result.Success)
        {
            _notificationService.ShowError("Error", result.ErrorMessage);
        }
    }

    private async Task DaikinShowStatus()
    {
        var infoResult = await _daikinFtxm25DataProvider.GetControlInfo();

        if (!infoResult.Success)
        {
            _notificationService.ShowError("Error", infoResult.ErrorMessage);
            return;
        }

        var tempResult = await _daikinFtxm25DataProvider.GetSensorInfo();

        if (!tempResult.Success)
        {
            _notificationService.ShowError("Error", tempResult.ErrorMessage);
            return;
        }

        var info = infoResult.Value;
        var temp = tempResult.Value;
        var isOn = info[Keys.Power] == Power.On;

        var mode = isOn
            ? $"⚡ {Mode.GetName(info[Keys.Mode])} to {info[Keys.Thermostat]} °C\n🌬️ Fans at {FanSpeed.GetName(info[Keys.FanSpeed])}"
            : "⚡ Power off";

        var temps = $"🌡️️ Room is {temp[Keys.InsideTemp]} °C\n🌳 Outside is {temp[Keys.OutsideTemp]} °C";

        _notificationService.ShowInfo("Daikin FTXM25", $"{mode}\n{temps}");
    }

    private async Task SmaShowStatus()
    {
        var yieldResult = await _smaSunnyBoyDataProvider.GetYield();

        if (!yieldResult.Success)
        {
            _notificationService.ShowError("Error", yieldResult.ErrorMessage);
            return;
        }

        var powerResult = await _homeWizardP1DataProvider.GetPower();

        if (!powerResult.Success)
        {
            _notificationService.ShowError("Error", powerResult.ErrorMessage);
            return;
        }

        var yield = yieldResult.Value;
        var power = powerResult.Value;

        var info = $"🌞 Yielding {yield} W";
        info += $"\n🏠 Consuming {yield + power.Import - power.Export} W";
        if (power.Import > 0) info += $"\n🔴 Drawing {power.Import} W";
        if (power.Export > 0) info += $"\n🟢 Injecting {power.Export} W";

        _notificationService.ShowInfo("SMA Sunny Boy", info);
    }

    private void ShowLogs()
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "log.txt");
            var psi = new ProcessStartInfo(logPath) { Verb = "open", UseShellExecute = true };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            const string msg = "Could not open log file.";
            _logger.LogError(ex, msg);
            _notificationService.ShowError("Error", msg);
        }
    }
}