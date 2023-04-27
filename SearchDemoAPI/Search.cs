using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Shared;
using Address = Shared.Address;

namespace API;

public class Search
{
    private readonly ILogger<Search> logger;
    private readonly IDistributedCache redisCache;
    private readonly SearchService searchService;

    public Search(ILogger<Search> log, IDistributedCache redisCache, SearchService searchService)
    {
        this.logger = log;
        this.redisCache = redisCache;
        this.searchService = searchService;
    }

    [FunctionName("Search")]
    [OpenApiOperation(operationId: "Run", tags: new[] { "Get Search" })]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "searchTerm", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The Search Term parameter")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<Address>), Description = "The OK response")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Description = "The Not Found response")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The Bad response")]
    public async Task<IActionResult> GetSearch(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
        HttpRequest req)
    {
        this.logger.LogInformation("C# HTTP trigger function processed a request.");

        string searchTerm = req.Query["searchTerm"];

        if (string.IsNullOrEmpty(searchTerm))
        {
            return new BadRequestErrorMessageResult($"The '{nameof(searchTerm)}' parameter is required.");
        }

        // Get the cache value
        string cacheValue = await this.redisCache.GetStringAsync(searchTerm);
        List<Address> addresses = new();

        if (!string.IsNullOrEmpty(cacheValue))
        {
            addresses.AddRange(JsonConvert.DeserializeObject<List<Address>>(cacheValue));
        }
        else
        {
            // If the searchTerm is not present in the cache, retrieve it from the search service
            List<Address> enumerable = await this.searchService.Search(searchTerm);
            addresses.AddRange(enumerable);
            if (addresses.Count == 0)
            {
                return new NotFoundResult();
            }

            // Store result from search service in the cache
            cacheValue = JsonConvert.SerializeObject(addresses);
            await this.redisCache.SetStringAsync(searchTerm, cacheValue, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)});
        }

        return new OkObjectResult(addresses);
    }

    [FunctionName("InsertAddress")]
    [OpenApiOperation(operationId: "Run", tags: new[] { "Insert Address" })]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Address), Description = "The Address parameter", Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The Bad response")]
    public async Task<IActionResult> InsertAddress(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequest req)
    {
        this.logger.LogInformation("C# HTTP trigger function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        Address address = JsonConvert.DeserializeObject<Address>(requestBody);

        if (address == null)
        {
            return new BadRequestErrorMessageResult($"The '{nameof(address)}' parameter is required.");
        }

        await this.searchService.UploadDocument(address);

        return new OkResult();
    }

    [FunctionName("InsertAddresses")]
    [OpenApiOperation(operationId: "Run", tags: new[] { "Insert Addresses" })]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(List<Address>), Description = "The Addresses parameter", Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The Bad response")]
    public async Task<IActionResult> InsertAddresses(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequest req)
    {
        this.logger.LogInformation("C# HTTP trigger function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        List<Address> addresses = JsonConvert.DeserializeObject<List<Address>>(requestBody);

        if (addresses == null || addresses.Count == 0)
        {
            return new BadRequestErrorMessageResult($"The '{nameof(addresses)}' parameter is required.");
        }

        await this.searchService.UploadDocuments(addresses);

        return new OkResult();
    }

    [FunctionName("InsertRandomAddress")]
    [OpenApiOperation(operationId: "Run", tags: new[] { "Insert Random Address" })]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "numberOfAddresses", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The Number of Addresses parameter")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<Address>), Description = "The OK response")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The Bad response")]
    public async Task<IActionResult> InsertRandomAddress(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequest req)
    {
        this.logger.LogInformation("C# HTTP trigger function processed a request.");

        string numberOfAddresses = req.Query["numberOfAddresses"];

        if (string.IsNullOrEmpty(numberOfAddresses))
        {
            return new BadRequestErrorMessageResult($"The '{nameof(numberOfAddresses)}' parameter is required.");
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
        return new OkObjectResult(addresses);
    }
}
