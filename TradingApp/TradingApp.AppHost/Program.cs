var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TradingApp>("tradingapp");

builder.Build().Run();
