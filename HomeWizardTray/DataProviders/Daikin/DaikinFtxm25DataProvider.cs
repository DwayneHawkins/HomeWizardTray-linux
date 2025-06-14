using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using HomeWizardTray.DataProviders.Daikin.Constants;
using Dic = System.Collections.Generic.Dictionary<string, string>;

namespace HomeWizardTray.DataProviders.Daikin;

internal sealed class DaikinFtxm25DataProvider
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _appSettings;
    private readonly string _baseUrl;

    public DaikinFtxm25DataProvider(HttpClient httpClient, AppSettings appSettings)
    {
        _httpClient = httpClient;
        _appSettings = appSettings;
        _baseUrl = $"http://{_appSettings.DaikinFtxm25IpAddress}";
    }

    public async Task<Dic> GetControlInfo()
    {
        var response = await _httpClient.GetStringAsync($"{_baseUrl}/aircon/get_control_info");
        var toQueryString = response.Replace(",", "&");
        var kvs = HttpUtility.ParseQueryString(toQueryString);
        return kvs.Cast<string>().ToDictionary(k => k, v => kvs[v]);
    }

    public async Task<Dic> GetSensorInfo()
    {
        var dataResponse = await _httpClient.GetStringAsync($"{_baseUrl}/aircon/get_sensor_info");
        var toQueryString = dataResponse.Replace(",", "&");
        var kvs = HttpUtility.ParseQueryString(toQueryString);
        return kvs.Cast<string>().ToDictionary(k => k, v => kvs[v]);
    }

    public async Task SetMax()
    {
        await SetSpecialMode(new Dic { [Keys.SpecialMode] = SpecialMode.Econo, [Keys.SpecialModeState] = SpecialModeState.Off });
        await SetSpecialMode(new Dic { [Keys.SpecialMode] = SpecialMode.Powerful, [Keys.SpecialModeState] = SpecialModeState.Off });

        await SetControlInfo(new Dic
        {
            [Keys.Power] = Power.On,
            [Keys.Mode] = Mode.Cooling,
            [Keys.Thermostat] = "18.0",
            [Keys.Humidity] = "0",
            [Keys.FanSpeed] = FanSpeed.Level5,
            [Keys.FanMotion] = FanMotion.None
        });
    }

    public async Task SetNormal()
    {
        await SetSpecialMode(new Dic { [Keys.SpecialMode] = SpecialMode.Econo, [Keys.SpecialModeState] = SpecialModeState.Off });
        await SetSpecialMode(new Dic { [Keys.SpecialMode] = SpecialMode.Powerful, [Keys.SpecialModeState] = SpecialModeState.Off });

        await SetControlInfo(new Dic
        {
            [Keys.Power] = Power.On,
            [Keys.Mode] = Mode.Cooling,
            [Keys.Thermostat] = "18.0",
            [Keys.Humidity] = "0",
            [Keys.FanSpeed] = FanSpeed.Level2,
            [Keys.FanMotion] = FanMotion.None
        });
    }

    public async Task SetEco()
    {
        await SetSpecialMode(new Dic { [Keys.SpecialMode] = SpecialMode.Econo, [Keys.SpecialModeState] = SpecialModeState.On });
        await SetSpecialMode(new Dic { [Keys.SpecialMode] = SpecialMode.Powerful, [Keys.SpecialModeState] = SpecialModeState.Off });

        await SetControlInfo(new Dic
        {
            [Keys.Power] = Power.On,
            [Keys.Mode] = Mode.Cooling,
            [Keys.Thermostat] = "18.0",
            [Keys.Humidity] = "0",
            [Keys.FanSpeed] = FanSpeed.Silent,
            [Keys.FanMotion] = FanMotion.None,
        });
    }

    public async Task SetDehumidify()
    {
        await SetSpecialMode(new Dic { [Keys.SpecialMode] = SpecialMode.Econo, [Keys.SpecialModeState] = SpecialModeState.Off });
        await SetSpecialMode(new Dic { [Keys.SpecialMode] = SpecialMode.Powerful, [Keys.SpecialModeState] = SpecialModeState.Off });

        await SetControlInfo(new Dic
        {
            [Keys.Power] = Power.On,
            [Keys.Mode] = Mode.Dehumidify,
            [Keys.Thermostat] = "18.0",
            [Keys.Humidity] = "0",
            [Keys.FanSpeed] = FanSpeed.Auto,
            [Keys.FanMotion] = FanMotion.None,
        });
    }

    public async Task SetOff()
    {
        var info = await GetControlInfo();
        
        if (info[Keys.Power] == Power.On)
        {
            info[Keys.Power] = Power.Off;
            await SetControlInfo(info);
        }
    }

    private async Task SetControlInfo(Dic dic)
    {
        var queryString = string.Join("&", dic.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var response = await _httpClient.PostAsync($"{_baseUrl}/aircon/set_control_info?{queryString}", null);
        response.EnsureSuccessStatusCode();
        var resonseBody = await response.Content.ReadAsStringAsync();
    }

    private async Task SetSpecialMode(Dic dic)
    {
        var queryString = string.Join("&", dic.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var response = await _httpClient.PostAsync($"{_baseUrl}/aircon/set_special_mode?{queryString}", null);
        response.EnsureSuccessStatusCode();
        var resonseBody = await response.Content.ReadAsStringAsync();
    }
}