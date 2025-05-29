using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using HomeWizardTray.DataProviders.Daikin.Constants;
using Newtonsoft.Json;
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

    public async Task<string> GetStatus()
    {
        var dic = await GetDic();

        return JsonConvert.SerializeObject(new Dic
            {
                [nameof(Keys.Power)] = Power.GetName(dic[Keys.Power]),
                [nameof(Keys.Mode)] = Mode.GetName(dic[Keys.Mode]),
                [nameof(Keys.Temperature)] = dic[Keys.Temperature],
                [nameof(Keys.Humidity)] = dic[Keys.Humidity],
                [nameof(Keys.FanSpeed)] = FanSpeed.GetName(dic[Keys.FanSpeed]),
                [nameof(Keys.FanMotion)] = FanMotion.GetName(dic[Keys.FanMotion]),
            })
            .Replace("{", "")
            .Replace("}", "")
            .Replace(":\"", " = ")
            .Replace("\"", "")
            .Replace(",", "\r\n");
    }

    private async Task<Dic> GetDic()
    {
        var dataResponse = await _httpClient.GetStringAsync($"{_baseUrl}/aircon/get_control_info");
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
            [Keys.Temperature] = "18.0",
            [Keys.Humidity] = "0",
            [Keys.FanSpeed] = FanSpeed.Level5,
            [Keys.FanMotion] = FanMotion.None
        });
    }

    public async Task SetLevel2()
    {
        await SetSpecialMode(new Dic { [Keys.SpecialMode] = SpecialMode.Econo, [Keys.SpecialModeState] = SpecialModeState.Off });
        await SetSpecialMode(new Dic { [Keys.SpecialMode] = SpecialMode.Powerful, [Keys.SpecialModeState] = SpecialModeState.Off });

        await SetControlInfo(new Dic
        {
            [Keys.Power] = Power.On,
            [Keys.Mode] = Mode.Cooling,
            [Keys.Temperature] = "18.0",
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
            [Keys.Temperature] = "18.0",
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
            [Keys.Temperature] = "18.0",
            [Keys.Humidity] = "0",
            [Keys.FanSpeed] = FanSpeed.Auto,
            [Keys.FanMotion] = FanMotion.None,
        });
    }

    public async Task SetOff()
    {
        var dic = await GetDic();
        dic[Keys.Power] = Power.Off;
        await SetControlInfo(dic);
    }

    private async Task SetControlInfo(Dic dic)
    {
        var queryString = string.Join("&", dic.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var response = await _httpClient.PostAsync($"{_baseUrl}/aircon/set_control_info?{queryString}", null);
        var resonseBody = await response.Content.ReadAsStringAsync();
    }

    private async Task SetSpecialMode(Dic dic)
    {
        var queryString = string.Join("&", dic.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var response = await _httpClient.PostAsync($"{_baseUrl}/aircon/set_special_mode?{queryString}", null);
        var resonseBody = await response.Content.ReadAsStringAsync();
    }
}