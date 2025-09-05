var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CircuitBreaker>("circuitbreaker");

builder.Build().Run();
