using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared;

namespace SWA1API
{
    public class Search
    {
        private readonly ILogger logger;
        private readonly SearchService searchService;

        public Search(ILoggerFactory loggerFactory, IConfiguration config)
        {
            this.logger = loggerFactory.CreateLogger<Search>();
            string searchServiceEndPoint = config.GetValue<string>("SearchServiceEndPoint");
            string adminApiKey = config.GetValue<string>("SearchServiceAdminApiKey");
            string indexName = config.GetValue<string>("SearchServiceIndexName");
            this.searchService = new SearchService(searchServiceEndPoint!, adminApiKey!, indexName!);
        }

        [Function("Search")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            this.logger.LogInformation("C# HTTP trigger function processed a request.");

            req.FunctionContext.BindingContext.BindingData.TryGetValue("searchTerm", out object? searchTermValue);

            string? searchTerm = searchTermValue?.ToString();
            if (string.IsNullOrEmpty(searchTerm))
            {
                HttpResponseData badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new { error = "The 'searchTerm' parameter is required." });
                return badRequestResponse;
            }

            // Get the cache value
            List<Address> addresses = new();

            // If the searchTerm is not present in the cache, retrieve it from the search service
            List<Address> enumerable = await this.searchService.Search(searchTerm);
            addresses.AddRange(enumerable);
            if (addresses.Count == 0)
            {
                HttpResponseData notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                return notFoundResponse;
            }

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(addresses);
            return response;
        }
    }
}
