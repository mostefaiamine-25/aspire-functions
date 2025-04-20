using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var functions = builder
    .AddAzureFunctionsProject<EmailPublisherFunction>("functions")
    .WithExternalHttpEndpoints();

builder.Build().Run();