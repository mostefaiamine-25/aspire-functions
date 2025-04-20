using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
                     .RunAsEmulator();

var queues = storage.AddQueues("Queues");

builder
    .AddAzureFunctionsProject<EmailPublisherFunction>("EmailFunction")
    .WithExternalHttpEndpoints()
    .WithHostStorage(storage)
    .WaitFor(queues);

builder
    .AddProject<Projects.AspireFunctionsClient>("EmailClient")
    .WithReference(queues)
    .WaitFor(queues);

builder.Build().Run();