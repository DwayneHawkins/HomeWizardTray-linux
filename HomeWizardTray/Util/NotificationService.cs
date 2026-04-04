using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace HomeWizardTray.Util;

internal sealed class NotificationService(ILogger<NotificationService> logger)
{
    private readonly string _iconPath = Path.Combine(AppContext.BaseDirectory, "sun.png");

    public void ShowInfo(string title, string message)
    {
        Show($"-a HomeWizardTray -i {_iconPath} -t 10000 \"{title}\" \"{message}\"");
    }

    public void ShowError(string title, string message)
    {
        Show($"-a HomeWizardTray -u critical \"{title}\" \"{message}\"");
    }

    private void Show(string arguments)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "notify-send", UseShellExecute = false, Arguments = arguments });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not invoke \"notify-send\".");
        }
    }
}
