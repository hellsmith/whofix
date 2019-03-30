using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WhoCanFixIt.DynamicsApi
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string dynamcisApiUrl = "https://m365x338761.api.crm4.dynamics.com/api/data/v9.1/";
            dynamcisApiUrl += "users";

            log.LogInformation("C# HTTP trigger function processed a request.");

            // URL Paramter => Wichtig für abfrage durch Teams etc.
            // string name = req.Query["name"];

        }
    }
}
