using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HomeWizardTray.DataProviders.Sma.Dto;
using HomeWizardTray.Util;
using Microsoft.Extensions.Logging;

namespace HomeWizardTray.DataProviders.Sma;

internal sealed class SmaSunnyBoyDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _appSettings;
    private readonly ILogger<SmaSunnyBoyDataProvider> _logger;
    private readonly string _baseUrl;

    private string _sid;

    public SmaSunnyBoyDataProvider(HttpClient httpClient, AppSettings appSettings, ILogger<SmaSunnyBoyDataProvider> logger)
    {
        _httpClient = httpClient;
        _appSettings = appSettings;
        _logger = logger;
        _baseUrl = $"https://{_appSettings.SmaSunnyBoyIpAddress}";
    }

    public async Task<ProviderResult<int>> GetYield()
    {
        try
        {
            await Login();

            ClearAndSetHeaders();

            var postData = new Dictionary<string, object>
            {
                { "keys", new List<string> { "6100_40263F00" } },
                { "destDev", new List<object>() }
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/dyn/getValues.json?sid={_sid}", postData);
            var responseBody = await response.Content.ReadAsStringAsync();

            await Logout();

            // Response keys are dynamic/device-specific, so we use recursive descent ".." to find "val".
            // Example response: { "result": { "0199-xxxxxCF8": { "6100_40263F00": { "1": [{ "val": 774 }] } } } }
            var wattToken = JObject.Parse(responseBody).SelectToken("$.result..val");
            var value = wattToken is null or { Type: JTokenType.Null } ? 0 : wattToken.Value<int>();

            return ProviderResult<int>.Ok(value);
        }
        catch (Exception ex)
        {
            const string msg = $"Could not get Sunny Boy yield ({nameof(SmaSunnyBoyDataProvider)}.{nameof(GetYield)}).";
            _logger.LogError(ex, msg);
            return ProviderResult<int>.Fail(msg);
        }
    }

    private async Task Login()
    {
        if (_sid != null) return;

        var postData = new Dictionary<string, string>
        {
            { "right", _appSettings.SmaSunnyBoyUser.ToString() },
            { "pass", _appSettings.SmaSunnyBoyPass }
        };

        ClearAndSetHeaders();

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/dyn/login.json", postData);
        _httpClient.DefaultRequestHeaders.Clear();
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);
        _sid = loginResponse?.Result?.Sid;

        if (_sid == null)
        {
            throw new Exception("Could not log in and retrieve sid from Sunny Boy.");
        }
    }

    // Method to logout from Sunny Boy. This is not stricly needed, but consider:
    // - The amount of simultaneous active sid keys in SMA device is limited.
    // - The SMA device will invalidate sid keys after some time.
    private async Task Logout()
    {
        if (_sid == null) return;
        ClearAndSetHeaders();
        await _httpClient.PostAsJsonAsync($"{_baseUrl}/dyn/logout.json?sid={_sid}", new { });
        _sid = null;
    }

    private void ClearAndSetHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Host", _appSettings.SmaSunnyBoyIpAddress);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.0; WOW64; rv:24.0) Gecko/20100101 Firefox/24.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "fr,fr-FR;q=0.8,en-US;q=0.5,en;q=0.3");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_baseUrl}/");

        if (_sid == null) return; // We have a sid, so construct and add the "auth" cookie

        var user = UserInfo.Get(_appSettings.SmaSunnyBoyUser);
        var role = new { bitMask = 4, title = _appSettings.SmaSunnyBoyUser, loginLevel = user.LoginLevel };
        var user443 = new { role, username = user.Tag, sid = _sid };

        var cookieValues = new Dictionary<string, object>
        {
            { "tmhDynamicLocale.locale", "en" },
            { "deviceUsr443", _appSettings.SmaSunnyBoyUser },
            { "deviceMode443", "PSK" },
            { "user443", user443 },
            { "deviceSid443", _sid },
        };

        static string escape(object obj) => Uri.EscapeDataString(JsonConvert.SerializeObject(obj));
        var cookie = cookieValues.Aggregate("", (x, y) => x + $"{y.Key}={escape(y.Value)}; ");

        _httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
    }
}
