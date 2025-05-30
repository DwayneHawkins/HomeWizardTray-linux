namespace HomeWizardTray.DataProviders.Daikin.Constants;

internal static class Power
{
    public const string Off = "0";
    public const string On = "1";

    public static string GetName(string value)
    {
        return value switch
        {
            "1" => "On",
            _ => "Off"
        };
    }
}