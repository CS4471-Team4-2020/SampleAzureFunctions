using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace FunctionApp1
{

    //Model of the table row.
    public class StocksInfo: TableEntity { 
        public string ParitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTime Date { get; set; }
        public double Price{ get; set; }
    }
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //params by URL Query variables.
            string pKey = req.Query["PartitionKey"];
            string rKey = req.Query["RowKey"];

            //params by JSON Body.
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            pKey = pKey ?? data?.PartitionKey;
            rKey = rKey ?? data?.RowKey;

            //Opening Azure Table Storage Connection
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse("CONNECTION STRING");
            CloudTableClient tableClient = cloudStorageAccount.CreateCloudTableClient();
            CloudTable cloudTable = tableClient.GetTableReference("stocksinfo");

            //Getting Data
            var row = RetrieveRecordAsync(cloudTable, pKey, rKey).Result;

            log.LogInformation("Printing: " + row.Date + " $" + row.Price);

            return (ActionResult)new OkObjectResult($"The price is, {row.Price}");
        }

        // Async call to retrieve.
        public static async Task<StocksInfo> RetrieveRecordAsync(CloudTable table, string partitionKey, string rowKey)
        {
            TableOperation tableOperation = TableOperation.Retrieve<StocksInfo>(partitionKey, rowKey);
            TableResult tableResult = await table.ExecuteAsync(tableOperation);
            return tableResult.Result as StocksInfo;
        }
    }
}
