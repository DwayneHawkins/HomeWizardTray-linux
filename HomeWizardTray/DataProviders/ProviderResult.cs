namespace HomeWizardTray.DataProviders;

internal record ProviderResult(bool Success, string ErrorMessage = null)
{
    public static ProviderResult Ok() => new(true);
    public static ProviderResult Fail(string errorMessage) => new(false, errorMessage);
}

internal record ProviderResult<T>(bool Success, T Value = default, string ErrorMessage = null)
{
    public static ProviderResult<T> Ok(T value) => new(true, Value: value);
    public static ProviderResult<T> Fail(string errorMessage) => new(false, ErrorMessage: errorMessage);
}