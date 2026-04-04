using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
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
                new("Normal", (_, _) => _commandQueue.Add(() => ExecuteDaikinCommand(_daikinFtxm25DataProvider.SetNormal, "Could not set Daikin to normal mode."))),
                new("Max", (_, _) => _commandQueue.Add(() => ExecuteDaikinCommand(_daikinFtxm25DataProvider.SetMax, "Could not set Daikin to max mode."))),
                new("Eco", (_, _) => _commandQueue.Add(() => ExecuteDaikinCommand(_daikinFtxm25DataProvider.SetEco, "Could not set Daikin to eco mode."))),
                new("Dehumidify", (_, _) => _commandQueue.Add(() => ExecuteDaikinCommand(_daikinFtxm25DataProvider.SetDehumidify, "Could not set Daikin to dehumidify mode."))),
                new("Off", (_, _) => _commandQueue.Add(() => ExecuteDaikinCommand(_daikinFtxm25DataProvider.SetOff, "Could not set Daikin to off.")))
            ]),
            new("Status", (_, _) => _commandQueue.Add(DaikinShowStatus)),
            new("-"),
            new("SUNNY BOY"),
            new("Status", (_, _) => _commandQueue.Add(SmaShowStatus)),
            new("-"),
            new("v" + Assembly.GetExecutingAssembly().GetName().Version),
            new("Logs", (_, _) => _commandQueue.Add(() =>
            {
                ShowLogs();
                return Task.CompletedTask;
            })),
            new("Quit", (_, _) =>
            {
                Dispose();
                Gtk.Application.Quit();
            })
        ]);
    }

    private async Task ExecuteDaikinCommand(Func<Task> action, string errorMessage)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            HandleException(ex, errorMessage);
        }
    }

    private async Task DaikinShowStatus()
    {
        try
        {
            var info = await _daikinFtxm25DataProvider.GetControlInfo();
            var temp = await _daikinFtxm25DataProvider.GetSensorInfo();

            var isOn = info[Keys.Power] == Power.On;

            var mode = isOn
                ? $"⚡ {Mode.GetName(info[Keys.Mode])} to {info[Keys.Thermostat]} °C\n🌬️ Fans at {FanSpeed.GetName(info[Keys.FanSpeed])}"
                : "⚡ Power off";

            var temps = $"🌡️️ Room is {temp[Keys.InsideTemp]} °C\n🌳 Outside is {temp[Keys.OutsideTemp]} °C";

            ShowNotification("Daikin FTXM25", $"{mode}\n{temps}");
        }
        catch (Exception ex)
        {
            HandleException(ex, "Could not get Daikin status.");
        }
    }

    private async Task SmaShowStatus()
    {
        try
        {
            var yield = await _smaSunnyBoyDataProvider.GetYield();
            var power = await _homeWizardP1DataProvider.GetPower();

            var info = $"🌞 Yielding {yield} W";
            info += $"\n🏠 Consuming {yield + power.Import - power.Export} W";
            if (power.Import > 0) info += $"\n🔴 Drawing {power.Import} W";
            if (power.Export > 0) info += $"\n🟢 Injecting {power.Export} W";

            ShowNotification("SMA Sunny Boy", info);
        }
        catch (Exception ex)
        {
            HandleException(ex, "Could not get Sunny Boy status.");
        }
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
            HandleException(ex, "Could not open log file.");
        }
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
            _logger.LogError(ex, "Could not invoke notify-send");
        }
    }

    private void HandleException(Exception ex, string message)
    {
        _logger.LogError(ex, "{Message}", message);
        ShowNotification("Error", message, isError: true);
    }

    public void Dispose()
    {
        _commandQueue.Dispose();
    }
}