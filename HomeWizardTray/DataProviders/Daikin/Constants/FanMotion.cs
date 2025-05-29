namespace HomeWizardTray.DataProviders.Daikin.Constants;

internal static class FanMotion
{
    public static string None = "0";
    public static string Vertical = "1";
    public static string Horizontal = "2";
    public static string Full = "3";

    public static string GetName(string value)
    {
        return value switch
        {
            "1" => nameof(Vertical),
            "2" => nameof(Horizontal),
            "3" => nameof(Full),
            _ => nameof(None)
        };
    }
}