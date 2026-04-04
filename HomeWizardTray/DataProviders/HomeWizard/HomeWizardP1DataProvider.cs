using System;
using System.Net.Http;
using System.Threading.Tasks;
using HomeWizardTray.DataProviders.HomeWizard.Dto;
using HomeWizardTray.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HomeWizardTray.DataProviders.HomeWizard;

internal sealed class HomeWizardP1DataProvider(HttpClient httpClient, AppSettings appSettings, ILogger<HomeWizardP1DataProvider> logger)
{
    // This device doesn't seem to support HTTPS well
    private readonly string _baseUrl = $"http://{appSettings.P1MeterIpAddress}";

    public async Task<ProviderResult<HomeWizardInfo>> GetPower()
    {
        try
        {
            var dataResponseJson = await httpClient.GetStringAsync($"{_baseUrl}/api/v1/data");
            var dataResponse = JsonConvert.DeserializeObject<DataResponse>(dataResponseJson);
            return ProviderResult<HomeWizardInfo>.Ok(new HomeWizardInfo(dataResponse.ActivePower));
        }
        catch (Exception ex)
        {
            const string msg = $"Could not get P1 meter data ({nameof(HomeWizardP1DataProvider)}.{nameof(GetPower)}).";
            logger.LogError(ex, msg);
            return ProviderResult<HomeWizardInfo>.Fail(msg);
        }
    }
}