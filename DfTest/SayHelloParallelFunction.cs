using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DfTest
{
    public static class SayHelloParallelFunction
    {
        [FunctionName(nameof(SayHelloToCitiesParallel))]
        public static async Task<List<string>> SayHelloToCitiesParallel(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            string[] cities = new string[]
            {
                "Tokyo",
                "Seattle",
                "Londone"
            };

            var tasks = cities.Select(city => context.CallActivityAsync<string>(nameof(SayHelloParallel), city));

            var outputs = new List<string>(await Task.WhenAll(tasks));

            return outputs;
        }

        [FunctionName(nameof(SayHelloParallel))]
        public static async Task<string> SayHelloParallel([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation("Saying hello to {name}.", name);

            await Task.Delay(15000);

            return $"Hello {name}!";
        }

        [FunctionName("SayHelloParallel_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(nameof(SayHelloToCitiesParallel), null);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}