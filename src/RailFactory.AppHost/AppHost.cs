var builder = DistributedApplication.CreateBuilder(args);

// Add project references so each microservice starts with the AppHost (see docs 08 and 10).

builder.Build().Run();
