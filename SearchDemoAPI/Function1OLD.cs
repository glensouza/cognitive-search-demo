//using System;
//using System.Linq;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Extensions.Http;
//using Microsoft.Extensions.Logging;
//using Shared;

//namespace API
//{
//    public static class Function1
//    {
//        [FunctionName("Function1")]
//        public static IActionResult Run(
//            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
//            ILogger log)
//        {
//            log.LogInformation("C# HTTP trigger function processed a request.");

//            Random randomNumber = new();
//            int temp = 0;

//            WeatherForecast[] result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
//            {
//                Date = DateTime.Now.AddDays(index),
//                TemperatureC = temp = randomNumber.Next(-20, 55),
//                Summary = GetSummary(temp)
//            }).ToArray();

//            return new OkObjectResult(result);
//        }

//        private static string GetSummary(int temp)
//        {
//            string summary = temp switch
//            {
//                >= 32 => "Hot",
//                <= 16 and > 0 => "Cold",
//                <= 0 => "Freezing",
//                _ => "Mild"
//            };

//            return summary;
//        }
//    }
//}