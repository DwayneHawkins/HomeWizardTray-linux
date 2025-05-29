namespace HomeWizardTray.DataProviders.Daikin.Constants;

internal static class Mode
{
    public static string Auto = "0";
    public static string Dehumidify = "2";
    public static string Cooling = "3";
    public static string Heating = "4";
    public static string FanOnly = "6";

    public static string GetName(string value)
    {
        return value switch
        {
            "2" => nameof(Dehumidify),
            "3" => nameof(Cooling),
            "4" => nameof(Heating),
            "6" => nameof(FanOnly),
            _ => nameof(Auto)
        };
    }
}