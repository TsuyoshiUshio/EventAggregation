using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Linq;

namespace EventAggregationSample
{
    public static class EventAggregation
    {
        [FunctionName("ParamOne")]
        public async static Task<IActionResult> ParamOne([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, 
            [OrchestrationClient] DurableOrchestrationClient client,
            TraceWriter log)
        {
           
            string variable = req.Query["variable"];
            string instanceId = req.Query["instanceId"];

            // string requestBody = new StreamReader(req.Body).ReadToEnd();
            // dynamic data = JsonConvert.DeserializeObject(requestBody);
            await client.RaiseEventAsync(instanceId, "GetParamOne", variable);
            return (ActionResult)new OkObjectResult($"We've receive the ParamOne variable: {variable} instanceId: {instanceId}");
        }

        [FunctionName("ParamTwo")]
        public async static Task<IActionResult> ParamTwo([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient client,
            TraceWriter log)
        {

            string variable = req.Query["variable"];
            string instanceId = req.Query["instanceId"];

            // string requestBody = new StreamReader(req.Body).ReadToEnd();
            // dynamic data = JsonConvert.DeserializeObject(requestBody);
            await client.RaiseEventAsync(instanceId, "GetParamTwo", variable);
            return (ActionResult)new OkObjectResult($"We've receive the ParamTwo variable: {variable} instanceId: {instanceId}");
        }



        [FunctionName("EventAggregation")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            var paramOne = context.WaitForExternalEvent<string>("GetParamOne");
            var paramTwo = context.WaitForExternalEvent<string>("GetParamTwo");

            await Task.WhenAll(paramOne, paramTwo);
            outputs.Add(paramOne.Result);
            outputs.Add(paramTwo.Result);

            var answer = await context.CallActivityAsync<string>("Add", outputs);

            return outputs;
        }

        [FunctionName("Add")]
        public static string Add([ActivityTrigger] List<string> parameter, TraceWriter log)
        {
            var sum = parameter.Sum(p => int.Parse(p));
            var answer = $"The answer is {sum}";
            log.Info(answer);
            return answer;
        }


        [FunctionName("EventAggregation_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("EventAggregation", null);

            log.Info($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}