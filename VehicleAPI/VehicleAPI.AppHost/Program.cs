var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.VehicleAPI>("vehicleapi");




builder.Build().Run();
