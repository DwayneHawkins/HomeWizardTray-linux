using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using HomeWizardTray.DataProviders;
using HomeWizardTray.DataProviders.Daikin;
using HomeWizardTray.DataProviders.Daikin.Constants;
using HomeWizardTray.DataProviders.HomeWizard;
using HomeWizardTray.DataProviders.Sma;
using HomeWizardTray.Util;
using Microsoft.Extensions.Logging;

namespace HomeWizardTray;

internal sealed class App : IDisposable
{
    private readonly HomeWizardP1DataProvider _homeWizardP1DataProvider;
    private readonly SmaSunnyBoyDataProvider _smaSunnyBoyDataProvider;
    private readonly DaikinFtxm25DataProvider _daikinFtxm25DataProvider;
    
    private readonly ILogger<App> _logger;
    private readonly CommandQueue _commandQueue;

    public App(
        HomeWizardP1DataProvider homeWizardP1DataProvider,
        SmaSunnyBoyDataProvider smaSunnyBoyDataProvider,
        DaikinFtxm25DataProvider daikinFtxm25DataProvider,
        CommandQueue commandQueue,
        ILogger<App> logger)
    {
        _homeWizardP1DataProvider = homeWizardP1DataProvider;
        _smaSunnyBoyDataProvider = smaSunnyBoyDataProvider;
        _daikinFtxm25DataProvider = daikinFtxm25DataProvider;
        _commandQueue = commandQueue;
        _commandQueue.OnCommand += async (_, evt) => await evt.Action();
        _logger = logger;
    }

    public void Start()
    {
        Gtk.Application.Init();
        var menu = BuildMenu();
        var iconPath = Path.Combine(AppContext.BaseDirectory, "sun.png");
        var trayIcon = new AyatanaAppIndicator(menu.Handle, iconPath);
        Gtk.Application.Run();
    }

    private Gtk.Menu BuildMenu()
    {
        return GtkMenuBuilder.Build(
        [
            new("DAIKIN"),
            new("Mode",
            [
                new("Normal", (_, _) => _commandQueue.Add(() => HandleProviderResult(_daikinFtxm25DataProvider.SetNormal()))),
                new("Max", (_, _) => _commandQueue.Add(() => HandleProviderResult(_daikinFtxm25DataProvider.SetMax()))),
                new("Eco", (_, _) => _commandQueue.Add(() => HandleProviderResult(_daikinFtxm25DataProvider.SetEco()))),
                new("Dehumidify", (_, _) => _commandQueue.Add(() => HandleProviderResult(_daikinFtxm25DataProvider.SetDehumidify()))),
                new("Off", (_, _) => _commandQueue.Add(() => HandleProviderResult(_daikinFtxm25DataProvider.SetOff())))
            ]),
            new("Status", (_, _) => _commandQueue.Add(DaikinShowStatus)),
            new("-"),
            new("SUNNY BOY"),
            new("Status", (_, _) => _commandQueue.Add(SmaShowStatus)),
            new("-"),
            new("v" + Assembly.GetExecutingAssembly().GetName().Version),
            new("Logs", (_, _) => _commandQueue.Add(ShowLogs)),
            new("Quit", (_, _) =>
            {
                Dispose();
                Gtk.Application.Quit();
            })
        ]);
    }

    private async Task HandleProviderResult(Task<ProviderResult> task)
    {
        var result = await task;
        if (!result.Success)
        {
            ShowNotification("Error", result.ErrorMessage, isError: true);
        }
    }

    private async Task DaikinShowStatus()
    {
        var infoResult = await _daikinFtxm25DataProvider.GetControlInfo();
        
        if (!infoResult.Success)
        {
            ShowNotification("Error", infoResult.ErrorMessage, isError: true);
            return;
        }

        var tempResult = await _daikinFtxm25DataProvider.GetSensorInfo();
        
        if (!tempResult.Success)
        {
            ShowNotification("Error", tempResult.ErrorMessage, isError: true);
            return;
        }

        var info = infoResult.Value;
        var temp = tempResult.Value;
        var isOn = info[Keys.Power] == Power.On;

        var mode = isOn
            ? $"⚡ {Mode.GetName(info[Keys.Mode])} to {info[Keys.Thermostat]} °C\n🌬️ Fans at {FanSpeed.GetName(info[Keys.FanSpeed])}"
            : "⚡ Power off";

        var temps = $"🌡️️ Room is {temp[Keys.InsideTemp]} °C\n🌳 Outside is {temp[Keys.OutsideTemp]} °C";

        ShowNotification("Daikin FTXM25", $"{mode}\n{temps}");
    }

    private async Task SmaShowStatus()
    {
        var yieldResult = await _smaSunnyBoyDataProvider.GetYield();
        
        if (!yieldResult.Success)
        {
            ShowNotification("Error", yieldResult.ErrorMessage, isError: true);
            return;
        }

        var powerResult = await _homeWizardP1DataProvider.GetPower();
        
        if (!powerResult.Success)
        {
            ShowNotification("Error", powerResult.ErrorMessage, isError: true);
            return;
        }

        var yield = yieldResult.Value;
        var power = powerResult.Value;

        var info = $"🌞 Yielding {yield} W";
        info += $"\n🏠 Consuming {yield + power.Import - power.Export} W";
        if (power.Import > 0) info += $"\n🔴 Drawing {power.Import} W";
        if (power.Export > 0) info += $"\n🟢 Injecting {power.Export} W";

        ShowNotification("SMA Sunny Boy", info);
    }

    private Task ShowLogs()
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
            ShowNotification("Error", msg, isError: true);
        }

        return Task.CompletedTask;
    }

    private void ShowNotification(string title, string message, bool isError = false)
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "sun.png");

            var psi = new ProcessStartInfo
            {
                FileName = "notify-send", UseShellExecute = false,
                Arguments = isError
                    ? $"-a HomeWizardTray -u critical \"{title}\" \"{message}\""
                    : $"-a HomeWizardTray -i {iconPath} -t 10000 \"{title}\" \"{message}\""
            };

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not invoke \"notify-send\".");
        }
    }

    public void Dispose()
    {
        _commandQueue.Dispose();
    }
}