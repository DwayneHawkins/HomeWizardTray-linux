using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Gdk;
using HomeWizardTray.DataProviders.Daikin;
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

    public App(
        HomeWizardP1DataProvider homeWizardP1DataProvider,
        SmaSunnyBoyDataProvider smaSunnyBoyDataProvider,
        DaikinFtxm25DataProvider daikinFtxm25DataProvider)
    {
        _homeWizardP1DataProvider = homeWizardP1DataProvider;
        _smaSunnyBoyDataProvider = smaSunnyBoyDataProvider;
        _daikinFtxm25DataProvider = daikinFtxm25DataProvider;
    }

    public void Start()
    {
        Gtk.Application.Init();
        var menu = BuildMenu();
        var iconPath = Path.GetDirectoryName(Environment.ProcessPath) + "/sun.png";
        var trayIcon = new AyatanaAppIndicator(menu.Handle, iconPath);
        Gtk.Application.Run();
    }

    private Gtk.Menu BuildMenu()
    {
        return GtkMenuBuilder.Build(
        [
            new("Daikin",
            [
                new("Power On", (s, e) => _daikinFtxm25DataProvider.SetLevel2()),
                new("Power Off", (s, e) => _daikinFtxm25DataProvider.SetOff()),
                new("-"),
                new("Set Max", (s, e) => _daikinFtxm25DataProvider.SetMax()),
                new("Set Normal", (s, e) => _daikinFtxm25DataProvider.SetLevel2()),
                new("Set Eco", (s, e) => _daikinFtxm25DataProvider.SetEco()),
                new("Set Dehumidify", (s, e) => _daikinFtxm25DataProvider.SetDehumidify()),
                new("-"),
                new("Show Status", (s, e) => ShowDaikinInfo())
            ]),
            new("Solar", 
            [
                new("Show Status", (s, e) => ShowSolarInfo()),
            ]),
            new("-"),
            new("Show Logs", (s, e) => ShowLogs()),
            new("Quit", (s, e) => Gtk.Application.Quit())
        ]);
    }

    private async Task ShowDaikinInfo()
    {
        try
        {
            var info = await _daikinFtxm25DataProvider.GetStatus();
            ShowDialog("Daikin", info);
        }
        catch (Exception ex)
        {
            HandleError(ex, "Could not get Daikin status.");
        }
    }

    private async Task ShowSolarInfo()
    {
        try
        {
            var yieldTask = _smaSunnyBoyDataProvider.GetYield();
            var powerTask = _homeWizardP1DataProvider.GetPower();
            await Task.WhenAll(yieldTask, powerTask);
            var yield = yieldTask.Result;
            var power = powerTask.Result;

            ShowDialog("Solar Status", $"""
                                        🌞 Solar yield: {yield} W

                                        ⚡ Power draw: {power.Import} W
                                        ⚡ Power injection: {power.Export} W

                                        🏠 Currrently using {yield + power.Import - power.Export} W
                                        """);
        }
        catch (Exception ex)
        {
            HandleError(ex, "Could not get EV status.");
        }
    }

    private static void ShowLogs()
    {
        try
        {
            var psi = new ProcessStartInfo("./log.txt") { Verb = "open", UseShellExecute = true };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            HandleError(ex, "Could not open log file.");
        }
    }

    private static void ShowDialog(string title, string text, Gtk.MessageType type = Gtk.MessageType.Info)
    {
        var dialog = new Gtk.MessageDialog(null, Gtk.DialogFlags.Modal, type, Gtk.ButtonsType.Ok, text);
        dialog.Title = title;
        var iconBytes = File.ReadAllBytes(Path.GetDirectoryName(Environment.ProcessPath) + "/sun.png");
        using var ms = new MemoryStream(iconBytes);
        dialog.Icon = new Pixbuf(ms);
        dialog.Run();
        dialog.Destroy();
    }
    
    private static void HandleError(Exception ex, string message)
    {
        Log.Error(ex, message);
        ShowDialog("Error", message + " " + ex.Message, Gtk.MessageType.Error);
    }
}