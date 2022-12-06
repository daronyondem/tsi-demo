using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Reflection;
using Humongous.Healthcare.Services;
using System.Linq;
using System.Collections.Specialized;

namespace serverless
{
    public class HealthCheck
    {
        private static Humongous.Healthcare.Services.ICosmosDbService _cosmosDbService;
        static HealthCheck()
        {
            if (_cosmosDbService == null)
            {
                string databaseName = "tsidb";
                string containerName = "tsicontainer";
                Microsoft.Azure.Cosmos.CosmosClient client = new Microsoft.Azure.Cosmos.CosmosClient(GetEnvironmentVariable("CosmosDBConnectionString"));
                CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);
                Microsoft.Azure.Cosmos.DatabaseResponse database = client.CreateDatabaseIfNotExistsAsync(databaseName).Result;
                database.Database.CreateContainerIfNotExistsAsync(containerName, "/id").Wait();

                _cosmosDbService = cosmosDbService;
            }
        }

        [FunctionName("HealthCheckList")]
        public static async Task<IActionResult> HealthCheckList(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "HealthCheck")] HttpRequest req,
            ILogger log)
        {
            return new OkObjectResult(await _cosmosDbService.GetMultipleAsync("SELECT * FROM c"));
        }

        [FunctionName("HealthCheckPerId")]
        public static async Task<IActionResult> HealthCheckPerId(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "HealthCheck/{id}")] HttpRequest req, string id,
            ILogger log)
        {
            return new OkObjectResult(await _cosmosDbService.GetAsync(id));
        }

        [FunctionName("HealthCheckDelete")]
        public static async Task<IActionResult> HealthCheckDelete(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "HealthCheck/{id}")] HttpRequest req, string id,
            ILogger log)
        {
            await _cosmosDbService.DeleteAsync(id);
            return new OkObjectResult("Deleted");
        }

        [FunctionName("HealthCheckAdd")]
        public static async Task<IActionResult> HealthCheckAdd(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = "HealthCheck")] HttpRequest req,
           ILogger log)
        {
            string requestBody = String.Empty;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            Humongous.Healthcare.Models.HealthCheck item = JsonConvert.DeserializeObject<Humongous.Healthcare.Models.HealthCheck>(requestBody);
            
            item.id = Guid.NewGuid().ToString();
            await _cosmosDbService.AddAsync(item);
            return new OkObjectResult(item);
        }

        [FunctionName("HealthCheckUpdate")]
        public static async Task<IActionResult> HealthCheckUpdate(
           [HttpTrigger(AuthorizationLevel.Function, "put", Route = "HealthCheck/{id}")] HttpRequest req,
           ILogger log)
        {
            string requestBody = String.Empty;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            Humongous.Healthcare.Models.HealthCheck item = JsonConvert.DeserializeObject<Humongous.Healthcare.Models.HealthCheck>(requestBody);

            await _cosmosDbService.UpdateAsync(item.id, item);
            return new OkObjectResult(item);
        }

        [FunctionName("GetStatus")]
        public static async Task<IActionResult> GetStatus(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var symptoms = new string[] { "Hair loss", "Internal bleeding", "Temporary blindness", "Ennui" };

            Humongous.Healthcare.Models.HealthCheck hc = new Humongous.Healthcare.Models.HealthCheck();
            hc.id = Guid.NewGuid().ToString();
            hc.PatientID = 5;
            hc.Date = DateTime.Now;
            hc.HealthStatus = "I feel unwell";
            hc.Symptoms = symptoms;
            return new OkObjectResult(hc);
        }
        private static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
