namespace HomeWizardTray.DataProviders.Daikin.Constants;

internal static class FanSpeed
{
    public static string Auto = "A";
    public static string Silent = "B";
    public static string Level1 = "3";
    public static string Level2 = "4";
    public static string Level3 = "5";
    public static string Level4 = "6";
    public static string Level5 = "7";

    public static string GetName(string value)
    {
        return value switch
        {
            "B" => nameof(Silent),
            "3" => nameof(Level1),
            "4" => nameof(Level2),
            "5" => nameof(Level3),
            "6" => nameof(Level4),
            "7" => nameof(Level5),
            _ => nameof(Auto)
        };
    }
}