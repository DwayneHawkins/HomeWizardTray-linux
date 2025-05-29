using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HomeWizardTray.Util;

internal sealed class AyatanaAppIndicator
{
    private IntPtr _appIndicator;
    
    public AyatanaAppIndicator(IntPtr menuHandle)
    {
        var iconFile = Path.GetDirectoryName(Environment.ProcessPath) + "/sun.png";
        _appIndicator = app_indicator_new("HomeWizardTray", iconFile, Category.Services);
        app_indicator_set_status(_appIndicator, Status.Active);
        app_indicator_set_menu(_appIndicator, menuHandle);
    }

    // install with yay
    // extra/libayatana-appindicator 0.5.94-1
    // however this version is obsolete, should instead use libayatana-appindicator-glib but this version does not work
    const string LIB_INDICATOR = "libayatana-appindicator3";

    [DllImport(LIB_INDICATOR)]
    private static extern IntPtr app_indicator_new(string id, string icon, Category category);

    [DllImport(LIB_INDICATOR)]
    private static extern void app_indicator_set_status(IntPtr indicator, Status status);

    [DllImport(LIB_INDICATOR)]
    private static extern void app_indicator_set_menu(IntPtr indicator, IntPtr menu);

    private enum Category
    {
        Status,
        Communications,
        Services,
        Hardware,
        Other
    }

    private enum Status
    {
        Passive,
        Active,
        Attention
    }
}