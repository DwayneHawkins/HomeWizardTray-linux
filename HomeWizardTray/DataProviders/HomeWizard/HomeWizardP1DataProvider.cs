using System;
using System.Net.Http;
using System.Threading.Tasks;
using HomeWizardTray.DataProviders.HomeWizard.Dto;
using Newtonsoft.Json;

namespace HomeWizardTray.DataProviders.HomeWizard;

internal sealed class HomeWizardP1DataProvider
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _appSettings;
    private readonly string _baseUrl;

    public HomeWizardP1DataProvider(HttpClient httpClient, AppSettings appSettings)
    {
        _httpClient = httpClient;
        _appSettings = appSettings;
        _baseUrl = $"http://{_appSettings.P1MeterIpAddress}";
    }

    public async Task<PowerInfo> GetPower()
    {
        var dataResponseJson = await _httpClient.GetStringAsync($"{_baseUrl}/api/v1/data");
        var dataResponse = JsonConvert.DeserializeObject<DataResponse>(dataResponseJson);
        return new PowerInfo(dataResponse.ActivePower);
    }

    public class PowerInfo
    {
        public uint Export { get; private set; }
        public uint Import { get; private set; }
        
        public PowerInfo(decimal activePower)
        {
            Import = activePower < 0 ? 0 : (uint)activePower;
            Export = activePower > 0 ? 0 : (uint)Math.Abs((int)activePower);
        }
    }
}