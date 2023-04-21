using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Shared;

namespace SWA2API;

public class Function1
{
    private readonly ILogger logger;
    private readonly HttpClient http;


    public Function1(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, HttpClient http)
    {
        this.http = http;
        this.logger = loggerFactory.CreateLogger<Function1>();
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

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        _ = response.WriteAsJsonAsync(await tempResponse.Content.ReadAsStringAsync());

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
