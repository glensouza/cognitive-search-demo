using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared;

namespace SWA2API;

public class WeatherForecastFunc
{
    private readonly ILogger logger;
    private readonly HttpClient http;

    public WeatherForecastFunc(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        this.logger = loggerFactory.CreateLogger<WeatherForecast>();
        this.http = new HttpClient
        {
            BaseAddress = new Uri(configuration["FunctionUrl"]),
            DefaultRequestHeaders =
            {
                { "x-functions-key", configuration["FunctionKey"] }
            }
        };
    }

    [Function("WeatherForecast")]
    public async Task<HttpResponseData > Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        this.logger.LogInformation("C# HTTP trigger function processed a request.");

        HttpResponseMessage tempResponse = await http.GetAsync("Function1");

        if (tempResponse.IsSuccessStatusCode)
        {
            this.logger.LogInformation("Okidoki");
        }
        else
        {
            this.logger.LogError($"{tempResponse.StatusCode} {tempResponse.ReasonPhrase}: ");
        }

        string responseText = await tempResponse.Content.ReadAsStringAsync();
        WeatherForecast[] forecasts = JsonConvert.DeserializeObject < WeatherForecast[]>(responseText);

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        _ = response.WriteAsJsonAsync(forecasts);

        return response;
    }

    private static string GetSummary(int temp)
    {
        string summary = temp switch
        {
            >= 32 => "Hot",
            <= 16 and > 0 => "Cold",
            <= 0 => "Freezing",
            _ => "Mild"
        };

        return summary;
    }
}
