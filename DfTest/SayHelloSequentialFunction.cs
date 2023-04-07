using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DfTest
{
    public static class SayHelloSequentialFunction
    {
        [FunctionName(nameof(SayHelloToCities))]
        public static async Task<List<string>> SayHelloToCities(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName(nameof(SayHello))]
        public static async Task<string> SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation("Saying hello to {name}.", name);
            await Task.Delay(15000);
            return $"Hello {name}!";
        }

        [FunctionName("SayHelloSequential_Start")]
        public static async Task<HttpResponseMessage> SayHelloStarter(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(nameof(SayHelloToCities), null);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        public class QueryParams
        {
            public string InstanceId { get; set; }
        }
        [FunctionName("HttpStop")]
        public static async Task<HttpResponseMessage> HttpStop(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null )] QueryParams queryParams,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = queryParams.InstanceId;

            // Function input comes from the request content.
            log.LogInformation("Stopping orchestration with ID = '{instanceId}'.", instanceId);

            await starter.TerminateAsync(instanceId, "user request");
            return new HttpResponseMessage();
        }
    }
}