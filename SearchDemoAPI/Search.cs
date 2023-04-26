//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Net;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Web.Http;
//using Azure.Core;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Extensions.Http;
//using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
//using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
//using Microsoft.Extensions.Caching.Distributed;
//using Microsoft.Extensions.Logging;
//using Microsoft.OpenApi.Models;
//using Newtonsoft.Json;
//using Shared;

//namespace API;

//public class Search
//{
//    private readonly ILogger<Search> logger;
//    private readonly IDistributedCache redisCache;
//    private readonly SearchService searchService;

//    public Search(ILogger<Search> log, IDistributedCache redisCache, SearchService searchService)
//    {
//        this.logger = log;
//        this.redisCache = redisCache;
//        this.searchService = searchService;
//    }

//    [FunctionName("Search")]
//    [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
//    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
//    [OpenApiParameter(name: "searchTerm", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Search Term** parameter")]
//    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<Address>), Description = "The OK response")]
//    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Description = "The Not Found response")]
//    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The Bad response")]
//    public async Task<IActionResult> GetSearch(
//        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
//        HttpRequest req)
//    {
//        logger.LogInformation("C# HTTP trigger function processed a request.");

//        string searchTerm = req.Query["name"];

//        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
//        dynamic data = JsonConvert.DeserializeObject(requestBody);
//        searchTerm = searchTerm ?? data?.name;

//        if (string.IsNullOrEmpty(searchTerm))
//        {
//            return new BadRequestErrorMessageResult($"The '{nameof(searchTerm)}' parameter is required.");
//        }

//        // Get the cache value
//        string cacheValue = await redisCache.GetStringAsync(searchTerm);
//        List<Address> addresses = new();

//        if (!string.IsNullOrEmpty(cacheValue))
//        {
//            addresses.AddRange(JsonConvert.DeserializeObject<List<Address>>(cacheValue));
//        }
//        else
//        {
//            // If the searchTerm is not present in the cache, retrieve it from the search service
//            addresses.AddRange(await searchService.Search(searchTerm));
//            if (addresses.Count == 0)
//            {
//                return new NotFoundResult();
//            }

//            // Store result from search service in the cache
//            cacheValue = JsonConvert.SerializeObject(addresses);
//            await redisCache.SetStringAsync(searchTerm, cacheValue);
//        }

//        return new OkObjectResult(addresses);
//    }
//}
