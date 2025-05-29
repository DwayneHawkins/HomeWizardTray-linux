using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HomeWizardTray.DataProviders.Sma.Dto;

namespace HomeWizardTray.DataProviders.Sma;

internal sealed class SmaSunnyBoyDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _appSettings;
    private readonly string _baseUrl;

    private string Sid { get; set; }
    private DateTime? SidStamp { get; set; }

    public SmaSunnyBoyDataProvider(HttpClient httpClient, AppSettings appSettings)
    {
        _httpClient = httpClient;
        _appSettings = appSettings;
        _baseUrl = $"https://{_appSettings.SunnyBoyIpAddress}";
    }

    private async Task Login()
    {
        var postData = new Dictionary<string, string>
        {
            { "right", _appSettings.SunnyBoyUser.ToString() },
            { "pass", _appSettings.SunnyBoyPass }
        };

        ClearAndSetHeaders();

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/dyn/login.json", postData);
        _httpClient.DefaultRequestHeaders.Clear();
        response.EnsureSuccessStatusCode();

        var responseContent = response.Content.ReadAsStringAsync().Result;

        var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);
        Sid = loginResponse?.Result?.Sid;
        if (Sid == null) throw new Exception($"Could not log in and retrieve sid from Sunny Boy. Response was: {responseContent}");
        SidStamp = DateTime.Now;
    }

    /// <summary>
    /// Method to logout from Sunny Boy. This is not stricly needed, but consider:
    /// - The amount of simultaneous active sid keys in SMA device is limited.
    /// - The SMA device will invalidate sid keys after some time.
    /// </summary>
    public async Task Logout()
    {
        if (Sid == null) return;
        ClearAndSetHeaders();
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/dyn/logout.json?sid={Sid}", new { });
        var responseContent = response.Content.ReadAsStringAsync().Result;
        //TODO check and log if not succeeded
        Sid = null;
        SidStamp = null;
    }

    public async Task<int> GetActivePower()
    {
        // Prevent using invalidated sids. TODO Figure out better way? Just use 1 hour for now.
        if (DateTime.Now - SidStamp > TimeSpan.FromMinutes(60)) await Logout();
            
        if (Sid == null) await Login();

        ClearAndSetHeaders();

        var postData = new Dictionary<string, object>
        {
            { "keys", new List<string> { "6100_40263F00" } },
            { "destDev", new List<object>() }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/dyn/getValues.json?sid={Sid}", postData);
        var responseBody = await response.Content.ReadAsStringAsync();
        // TODO if (responseBody.Contains("err")) throw...

        var watt = responseBody.Split(':').Last().Split('}').First();
        return watt == "null" ? 0 : int.Parse(watt);
    }

    private void ClearAndSetHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Host", _appSettings.SunnyBoyIpAddress);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.0; WOW64; rv:24.0) Gecko/20100101 Firefox/24.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "fr,fr-FR;q=0.8,en-US;q=0.5,en;q=0.3");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_baseUrl}/");

        if (Sid != null) // We have a sid, so construct and add the "auth" cookie
        {
            var user = UserInfo.Get(_appSettings.SunnyBoyUser);
            var role = new { bitMask = 4, title = _appSettings.SunnyBoyUser, loginLevel = user.LoginLevel };
            var user443 = new { role, username = user.Tag, sid = Sid };

            var cookieValues = new Dictionary<string, object>
            {
                { "tmhDynamicLocale.locale", "en" },
                { "deviceUsr443", _appSettings.SunnyBoyUser },
                { "deviceMode443", "PSK" },
                { "user443", user443 },
                { "deviceSid443", Sid },
            };

            static string escape(object obj) => Uri.EscapeDataString(JsonConvert.SerializeObject(obj));
            var cookie = cookieValues.Aggregate("", (x, y) => x + $"{y.Key}={escape(y.Value)}; ");

            _httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
        }
    }
}