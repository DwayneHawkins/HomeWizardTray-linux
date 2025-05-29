namespace HomeWizardTray.DataProviders.Daikin.Constants;

internal static class Power
{
    public static string Off = "0";
    public static string On = "1";

    public static string GetName(string value)
    {
        return value switch
        {
            "1" => nameof(On),
            _ => nameof(Off)
        };
    }
}