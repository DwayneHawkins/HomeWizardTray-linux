using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using HomeWizardTray.DataProviders.Daikin;
using HomeWizardTray.DataProviders.Daikin.Constants;
using HomeWizardTray.DataProviders.HomeWizard;
using HomeWizardTray.DataProviders.Sma;
using HomeWizardTray.Util;
using Serilog;

namespace HomeWizardTray;

internal sealed class App
{
    private readonly HomeWizardP1DataProvider _homeWizardP1DataProvider;
    private readonly SmaSunnyBoyDataProvider _smaSunnyBoyDataProvider;
    private readonly DaikinFtxm25DataProvider _daikinFtxm25DataProvider;
    private readonly CommandQueue _commandQueue;

    public App(
        HomeWizardP1DataProvider homeWizardP1DataProvider,
        SmaSunnyBoyDataProvider smaSunnyBoyDataProvider,
        DaikinFtxm25DataProvider daikinFtxm25DataProvider)
    {
        _homeWizardP1DataProvider = homeWizardP1DataProvider;
        _smaSunnyBoyDataProvider = smaSunnyBoyDataProvider;
        _daikinFtxm25DataProvider = daikinFtxm25DataProvider;

        _commandQueue = new CommandQueue();
        _commandQueue.OnCommand += OnCommand;
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
                new("Normal", (_, _) => _commandQueue.Add(nameof(DaikinSetNormal))),
                new("Max", (_, _) => _commandQueue.Add(nameof(DaikinSetMax))),
                new("Eco", (_, _) => _commandQueue.Add(nameof(DaikinSetEco))),
                new("Dehumidify", (_, _) => _commandQueue.Add(nameof(DaikinSetDehumidify))),
                new("Off", (_, _) => _commandQueue.Add(nameof(DaikinSetOff)))
            ]),
            new("Status", (_, _) => _commandQueue.Add(nameof(DaikinShowStatus))),
            new("-"),
            new("SUNNY BOY"),
            new("Status", (_, _) => _commandQueue.Add(nameof(SmaShowStatus))),
            new("-"),
            new("Logs", (_, _) => _commandQueue.Add(nameof(ShowLogs))),
            new("Quit", (_, _) => Gtk.Application.Quit())
        ]);
    }

    private async void OnCommand(object sender, CommandQueueEventArgs evt)
    {
        switch (evt.Command)
        {
            case nameof(DaikinSetNormal): await DaikinSetNormal(); break;
            case nameof(DaikinSetMax): await DaikinSetMax(); break;
            case nameof(DaikinSetEco): await DaikinSetEco(); break;
            case nameof(DaikinSetDehumidify): await DaikinSetDehumidify(); break;
            case nameof(DaikinSetOff): await DaikinSetOff(); break;
            case nameof(DaikinShowStatus): await DaikinShowStatus(); break;
            case nameof(SmaShowStatus): await SmaShowStatus(); break;
            case nameof(ShowLogs): ShowLogs(); break;
        }
    }

    private async Task DaikinSetNormal()
    {
        try
        {
            await _daikinFtxm25DataProvider.SetNormal();
        }
        catch (Exception ex)
        {
            HandleException(ex, "Could not set Daikin to normal mode.");
        }
    }

    private async Task DaikinSetMax()
    {
        try
        {
            await _daikinFtxm25DataProvider.SetMax();
        }
        catch (Exception ex)
        {
            HandleException(ex, "Could not set Daikin to max mode.");
        }
    }

    private async Task DaikinSetDehumidify()
    {
        try
        {
            await _daikinFtxm25DataProvider.SetDehumidify();
        }
        catch (Exception ex)
        {
            HandleException(ex, "Could not set Daikin to dehumidify mode.");
        }
    }

    private async Task DaikinSetOff()
    {
        try
        {
            await _daikinFtxm25DataProvider.SetOff();
        }
        catch (Exception ex)
        {
            HandleException(ex, "Could not set Daikin to off.");
        }
    }

    private async Task DaikinSetEco()
    {
        try
        {
            await _daikinFtxm25DataProvider.SetEco();
        }
        catch (Exception ex)
        {
            HandleException(ex, "Could not set Daikin to eco mode.");
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
            if (power.Import > 0) info += $"\n🔻 Drawing {power.Import} W";
            info += $"\n🏠 Consuming {yield + power.Import - power.Export} W";
            if (power.Export > 0) info += $"\n👍🏻 Injecting {power.Export} W"; 

            ShowNotification("SMA Sunny Boy", info);
        }
        catch (Exception ex)
        {
            HandleException(ex, "Could not get Sunny Boy status.");
        }
    }

    private static void ShowLogs()
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

    private static void ShowNotification(string title, string message, bool isError = false)
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
            Log.Error(ex, "Could not invoke notify-send.");
        }
    }

    private static void HandleException(Exception ex, string message)
    {
        Log.Error(ex, message);
        ShowNotification("Error", message, isError: true);
    }
}