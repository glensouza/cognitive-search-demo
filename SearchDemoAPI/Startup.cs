using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Shared;

[assembly: FunctionsStartup(typeof(API.Startup))]
namespace API;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        string searchServiceEndPoint = Environment.GetEnvironmentVariable("SearchServiceEndPoint");
        string adminApiKey = Environment.GetEnvironmentVariable("SearchServiceAdminApiKey");
        string indexName = Environment.GetEnvironmentVariable("SearchServiceIndexName");
        builder.Services.AddSingleton((s) => new SearchService(searchServiceEndPoint!, adminApiKey!, indexName!));
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = Environment.GetEnvironmentVariable("RedisCacheConnectionString");
            options.InstanceName = "AddressSearch";
        });
    }
}
