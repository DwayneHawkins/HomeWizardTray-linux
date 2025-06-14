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
            "B" => "Silent️",
            "3" => "20%",
            "4" => "40%",
            "5" => "60%",
            "6" => "80%",
            "7" => "100%",
            _ => "Auto"
        };
    }
}