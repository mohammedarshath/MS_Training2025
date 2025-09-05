// See https://aka.ms/new-console-template for more information
using CamundaConsoleApp;
using Google.Apis.Auth.OAuth2;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Builder;
using Zeebe.Client.Api.Worker;
using Zeebe.Client.Impl.Builder;

// load appsettings.json
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var zeebe = config.GetSection("Zeebe").Get<ZeebeSettings>();



IZeebeClient zeebeClient = CamundaCloudClientBuilder
                  .Builder()
                  .UseClientId(zeebe?.ClientId)
                  .UseClientSecret(zeebe?.ClientSecret)
                  .UseContactPoint(zeebe?.GatewayAddress)
                  .Build();



Console.WriteLine("Connecting to Camunda Cloud...");

//var topology = await zeebeClient.TopologyRequest().Send();
Console.WriteLine("Connected to Camunda Cloud!");

var jobType = "fetch-inventory"; // must match your BPMN service task type


//Console.WriteLine($"Cluster Size: {topology.Brokers.Count}");
var processInstanceResponse = zeebeClient
                .NewCreateProcessInstanceCommand()
                .BpmnProcessId("Inventory_Process")                
                .LatestVersion()
                .Send();

Console.WriteLine("Process Instance has been started!");
//var processInstanceKey = processInstanceResponse.Result.ProcessInstanceKey;
//Console.WriteLine($"Process Instance Key: {processInstanceKey}");

var _worker = zeebeClient.NewWorker()
    .JobType(jobType)
    .Handler(HandleJob)
    .MaxJobsActive(5)                   // optional
    //.Name("aspnet-worker")              //In .NET, Name is a property setter, not a fluent method
    .Timeout(TimeSpan.FromSeconds(300))  // optional
    .Open();



Console.WriteLine($"Worker started for job type '{jobType}'. Press Ctrl+C to exit.");
//await Task.Delay(Timeout.InfiniteTimeSpan);

// ---- handler ----
static async void HandleJob(IJobClient jobClient, Zeebe.Client.Api.Responses.IJob job)
{
    // Example: read variables, do work, optionally set new variables
    var vars = job.Variables; // JSON
    var data = string.IsNullOrEmpty(vars) ? new { } : JsonSerializer.Deserialize<dynamic>(vars);

    Console.WriteLine($"Got job {job.Key} of type {job.Type} with vars: {vars}");

    // Do your work here (HTTP call, DB write, email, etc.)
    // Throw an exception to let Zeebe retry based on retries/backoff.

    // If you don’t use .AutoCompletion(), you can complete explicitly:
     await jobClient.NewCompleteJobCommand(job.Key)
        .Variables(JsonSerializer.Serialize(new { result = "ok" }))
        .Send();
}