namespace HomeWizardTray.DataProviders.Daikin.Constants;

internal static class FanMotion
{
    public const string None = "0";
    public const string Vertical = "1";
    public const string Horizontal = "2";
    public const string Full = "3";

    public static string GetName(string value)
    {
        return value switch
        {
            "1" => "Vertical",
            "2" => "Horizontal",
            "3" => "Full",
            _ => "None"
        };
    }
}