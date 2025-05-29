using System;
using Log = Serilog.Log;
using HomeWizardTray.DataProviders.Sma;
using Microsoft.Extensions.Configuration;

namespace HomeWizardTray;

internal sealed class AppSettings
{
    public string DaikinFtxm25IpAddress { get; set; }
    public string P1MeterIpAddress { get; set; }
    public string SmaSunnyBoyIpAddress { get; set; }
    public UserType SmaSunnyBoyUser { get; set; }
    public string SmaSunnyBoyPass { get; set; }

    public AppSettings(IConfiguration config)
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
            Log.Error(ex, ex.Message);
            throw;
        }
    }
}