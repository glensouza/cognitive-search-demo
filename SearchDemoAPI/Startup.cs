using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        //ILoggerFactory loggerFactory = new LoggerFactory();
        //ILogger searchLogger = loggerFactory.CreateLogger("Startup");
        //builder.Services.AddSingleton((s) => new SearchService(searchServiceEndPoint!, adminApiKey!, indexName!, searchLogger));
        //builder.Services.AddSingleton((s) => new SearchService(searchServiceEndPoint!, adminApiKey!, indexName!));
        //builder.Services.AddStackExchangeRedisCache(options =>
        //{
        //    options.Configuration = Environment.GetEnvironmentVariable("RedisCache");
        //    options.InstanceName = "AddressSearch";
        //});
    }
}
