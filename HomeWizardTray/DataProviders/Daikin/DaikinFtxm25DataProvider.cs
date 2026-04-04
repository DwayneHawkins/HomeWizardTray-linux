using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HomeWizardTray.DataProviders.Daikin.Constants;
using HomeWizardTray.Util;
using Microsoft.Extensions.Logging;
using Dic = System.Collections.Generic.Dictionary<string, string>;

namespace HomeWizardTray.DataProviders.Daikin;

internal sealed class DaikinFtxm25DataProvider(HttpClient httpClient, AppSettings appSettings, ILogger<DaikinFtxm25DataProvider> logger)
{
    // This device doesn't seem to support HTTPS well
    private readonly string _baseUrl = $"http://{appSettings.DaikinFtxm25IpAddress}";

    public async Task<ProviderResult<Dic>> GetControlInfo()
    {
        try
        {
            var response = await httpClient.GetStringAsync($"{_baseUrl}/aircon/get_control_info");
            return ProviderResult<Dic>.Ok(ParseResponse(response));
        }
        catch (Exception ex)
        {
            const string msg = $"Could not get Daikin control info ({nameof(DaikinFtxm25DataProvider)}.{nameof(GetControlInfo)}).";
            logger.LogError(ex, msg);
            return ProviderResult<Dic>.Fail(msg);
        }
    }

    public async Task<ProviderResult<Dic>> GetSensorInfo()
    {
        try
        {
            var response = await httpClient.GetStringAsync($"{_baseUrl}/aircon/get_sensor_info");
            return ProviderResult<Dic>.Ok(ParseResponse(response));
        }
        catch (Exception ex)
        {
            const string msg = $"Could not get Daikin sensor info ({nameof(DaikinFtxm25DataProvider)}.{nameof(GetSensorInfo)}).";
            logger.LogError(ex, msg);
            return ProviderResult<Dic>.Fail(msg);
        }
    }

    public async Task<ProviderResult> SetMax()
    {
        try
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

            return ProviderResult.Ok();
        }
        catch (Exception ex)
        {
            const string msg = $"Could not set Daikin to max mode ({nameof(DaikinFtxm25DataProvider)}.{nameof(SetMax)}).";
            logger.LogError(ex, msg);
            return ProviderResult.Fail(msg);
        }
    }

    public async Task<ProviderResult> SetNormal()
    {
        try
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

            return ProviderResult.Ok();
        }
        catch (Exception ex)
        {
            const string msg = $"Could not set Daikin to normal mode ({nameof(DaikinFtxm25DataProvider)}.{nameof(SetNormal)}).";
            logger.LogError(ex, msg);
            return ProviderResult.Fail(msg);
        }
    }

    public async Task<ProviderResult> SetEco()
    {
        try
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
                [Keys.FanMotion] = FanMotion.None
            });

            return ProviderResult.Ok();
        }
        catch (Exception ex)
        {
            const string msg = $"Could not set Daikin to eco mode ({nameof(DaikinFtxm25DataProvider)}.{nameof(SetEco)}).";
            logger.LogError(ex, msg);
            return ProviderResult.Fail(msg);
        }
    }

    public async Task<ProviderResult> SetDehumidify()
    {
        try
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
                [Keys.FanMotion] = FanMotion.None
            });

            return ProviderResult.Ok();
        }
        catch (Exception ex)
        {
            const string msg = $"Could not set Daikin to dehumidify mode ({nameof(DaikinFtxm25DataProvider)}.{nameof(SetDehumidify)}).";
            logger.LogError(ex, msg);
            return ProviderResult.Fail(msg);
        }
    }

    public async Task<ProviderResult> SetOff()
    {
        try
        {
            var info = await httpClient.GetStringAsync($"{_baseUrl}/aircon/get_control_info");
            var dic = ParseResponse(info);

            if (dic[Keys.Power] == Power.On)
            {
                dic[Keys.Power] = Power.Off;
                await SetControlInfo(dic);
            }

            return ProviderResult.Ok();
        }
        catch (Exception ex)
        {
            const string msg = $"Could not set Daikin to off ({nameof(DaikinFtxm25DataProvider)}.{nameof(SetOff)}).";
            logger.LogError(ex, msg);
            return ProviderResult.Fail(msg);
        }
    }

    private async Task SetControlInfo(Dic dic)
    {
        var queryString = string.Join("&", dic.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var response = await httpClient.PostAsync($"{_baseUrl}/aircon/set_control_info?{queryString}", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task SetSpecialMode(Dic dic)
    {
        var queryString = string.Join("&", dic.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var response = await httpClient.PostAsync($"{_baseUrl}/aircon/set_special_mode?{queryString}", null);
        response.EnsureSuccessStatusCode();
    }

    // Daikin responses are comma-separated key=value pairs, e.g. "ret=OK,pow=1,mode=3,stemp=18.0"
    private static Dic ParseResponse(string response)
    {
        return response.Split(',')
            .Select(x => x.Split('=', 2))
            .ToDictionary(x => x[0], parts => parts[1]);
    }
}