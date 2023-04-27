using System.Net;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using System.Web.Http;
using Bogus.DataSets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Address = Shared.Address;
using Shared;
using Microsoft.Extensions.Configuration;

namespace SWA1API
{
    public class InsertRandomAddress
    {
        private readonly ILogger logger;
        private readonly SearchService searchService;

        public InsertRandomAddress(ILoggerFactory loggerFactory, IConfiguration config)
        {
            this.logger = loggerFactory.CreateLogger<InsertRandomAddress>();
            string searchServiceEndPoint = config.GetValue<string>("SearchServiceEndPoint");
            string adminApiKey = config.GetValue<string>("SearchServiceAdminApiKey");
            string indexName = config.GetValue<string>("SearchServiceIndexName");
            this.searchService = new SearchService(searchServiceEndPoint!, adminApiKey!, indexName!);
        }

        [Function("InsertRandomAddress")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequestData req)
        {
            this.logger.LogInformation("C# HTTP trigger function processed a request.");

            req.FunctionContext.BindingContext.BindingData.TryGetValue("numberOfAddresses", out object? numberOfAddressesValue);

            string? numberOfAddresses = numberOfAddressesValue?.ToString();
            if (string.IsNullOrEmpty(numberOfAddresses))
            {
                HttpResponseData badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new { error = "The 'numberOfAddresses' parameter is required." });
                return badRequestResponse;
            }

            if (!int.TryParse(numberOfAddresses, out int howMany))
            {
                howMany = 5;
            }

            Faker<Address> newAddresses = new Faker<Address>()
                .CustomInstantiator(f => new Address(Guid.NewGuid().ToString()))
                .RuleFor(p => p.Line1, f => f.Address.StreetAddress())
                .RuleFor(p => p.Line2, f => f.Address.BuildingNumber())
                .RuleFor(p => p.City, f => f.Address.City())
                .RuleFor(p => p.State, f => f.Address.StateAbbr())
                .RuleFor(p => p.PostalCode, f => f.Address.ZipCode())
                .FinishWith((f, u) => { this.logger.LogInformation("Address Created! {0}", JsonConvert.SerializeObject(u)); });

            List<Address> addresses = newAddresses.Generate(howMany);
            await this.searchService.UploadDocuments(addresses);

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(addresses);
            return response;
        }
    }
}
