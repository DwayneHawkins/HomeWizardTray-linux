namespace HomeWizardTray.DataProviders.Daikin.Constants;

internal static class FanSpeed
{
    public const string Auto = "A";
    public const string Silent = "B";
    public const string Level1 = "3";
    public const string Level2 = "4";
    public const string Level3 = "5";
    public const string Level4 = "6";
    public const string Level5 = "7";

    public static string GetName(string value)
    {
        return value switch
        {
            "B" => "Silent",
            "3" => "▫️️▪️▪️▪️▪️",
            "4" => "▫️▫️️▪️▪️▪️",
            "5" => "▫️▫️▫️️▪️▪️",
            "6" => "▫️▫️▫️▫️️▪️",
            "7" => "▫️▫️▫️▫️▫️️",
            _ => "Auto"
        };
    }
}