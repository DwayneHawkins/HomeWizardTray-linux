using System;
using HomeWizardTray.DataProviders.Sma;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HomeWizardTray.Util;

internal sealed class AppSettings
{
    public string DaikinFtxm25IpAddress { get; set; }
    public string P1MeterIpAddress { get; set; }
    public string SmaSunnyBoyIpAddress { get; set; }
    public UserType SmaSunnyBoyUser { get; set; }
    public string SmaSunnyBoyPass { get; set; }

    public AppSettings(IConfiguration config, ILogger<AppSettings> logger)
    {
        try
        {
            config.Bind(this);

            if (string.IsNullOrWhiteSpace(SmaSunnyBoyPass))
            {
                throw new Exception($"{nameof(SmaSunnyBoyPass)} can not be null or empty. Please check the app settings file.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not bind AppSettings.");
            throw;
        }
    }
}