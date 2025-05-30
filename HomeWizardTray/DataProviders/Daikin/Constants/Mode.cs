namespace HomeWizardTray.DataProviders.Daikin.Constants;

internal static class Mode
{
    public const string Auto = "0";
    public const string Dehumidify = "2";
    public const string Cooling = "3";
    public const string Heating = "4";
    public const string FanOnly = "6";

    public static string GetName(string value)
    {
        return value switch
        {
            "2" => "Dehumidify",
            "3" => "Cooling",
            "4" => "Heating",
            "6" => "Fan Only",
            _ => "Auto"
        };
    }
}