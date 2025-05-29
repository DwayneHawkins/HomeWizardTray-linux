using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HomeWizardTray.DataProviders.HomeWizard;
using HomeWizardTray.DataProviders.Sma;
using HomeWizardTray.DataProviders.Daikin;
using HomeWizardTray.Util;

namespace HomeWizardTray;

internal sealed class App
{
    private readonly HomeWizardP1DataProvider _homeWizardP1DataProvider;
    private readonly SmaSunnyBoyDataProvider _smaSunnyBoyDataProvider;
    private readonly DaikinFtxm25DataProvider _daikinFtxm25DataProvider;
    private AyatanaAppIndicator _trayIcon;
    private Gtk.Menu _menu;

    public App(
        HomeWizardP1DataProvider homeWizardP1DataProvider,
        SmaSunnyBoyDataProvider smaSunnyBoyDataProvider,
        DaikinFtxm25DataProvider daikinFtxm25DataProvider)
    {
        _homeWizardP1DataProvider = homeWizardP1DataProvider;
        _smaSunnyBoyDataProvider = smaSunnyBoyDataProvider;
        _daikinFtxm25DataProvider = daikinFtxm25DataProvider;

        _menu = BuildMenu();
        _trayIcon = new AyatanaAppIndicator(_menu.Handle);
    }

    private Gtk.Menu BuildMenu()
    {
        return GtkMenuBuilder.Build(
        [
            new("Daikin Airco",
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
            new("Show EV Info", (s, e) => ShowEvInfo()),
            new("-"),
            new("Show logs", (s, e) => ShowLogs()),
            new("Quit", (s, e) => Gtk.Application.Quit())
        ]);
    }

    private async Task ShowDaikinInfo()
    {
        var info = await _daikinFtxm25DataProvider.GetStatus();
        ShowDialog("Daikin Airco", info);
    }

    private async Task ShowEvInfo()
    {
        var activePower = await _homeWizardP1DataProvider.GetActivePower();
        var solarPower = await _smaSunnyBoyDataProvider.GetActivePower();
        await _smaSunnyBoyDataProvider.Logout();

        var info = $"""
                    🌞 Solar yield: {solarPower} W
                    ⚡ Power usage: {solarPower + activePower} W
                    ⚡ Power draw: {(activePower < 0 ? 0 : activePower)} W
                    ⚡ Power inject: {(activePower > 0 ? 0 : Math.Abs(activePower))} W
                    """;

        ShowDialog("EV Info", info);
    }

    private static void ShowLogs()
    {
        var info = new ProcessStartInfo("./log.txt") { Verb = "open", UseShellExecute = true };
        Process.Start(info);
    }

    private static void ShowDialog(string title, string text)
    {
        var dialog = new Gtk.MessageDialog(null, Gtk.DialogFlags.Modal, Gtk.MessageType.Info, Gtk.ButtonsType.Ok, text);
        dialog.Title = title;
        dialog.Run();
        dialog.Destroy();
    }
}