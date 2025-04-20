# .NET Aspire Integration with Azure Functions

[.Net Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) is one of the major additions to the .NET ecosystem since the release of .NET core itself. 

.NET Aspire provides the packages and the tools required for dev-time orchestration of not only applications but also services such as databases or storage system.

One of the newest - and the nicest - additions to .NET Aspire is the ability to include Azure Functions to the orchestration and access the Azure function in the dashboard as any other resource.

# Architecture
The solutions relies on a minimal architecture where a client application written using Blazor server publishes a queue message (a serialized email) that the azure function will pick to send it (sending is just a log message here). 

![architecture](/images/architecture.png)

# Making it work
## The Client
### Configuring the Queue
The host builder should be configure to provide the ```QueueServiceClient``` to the consuming pages and/or services. For this, in the Program.cs, we should do the following:
```cs
builder.AddAzureQueueClient("Queues")
```
This means the ```QueueServiceClient``` is added to DI. 
However, we will have a problem, when publishing to the queue, the azure function will not be able to read the message because it expect the messages to be **base64** encoded.

To fix this problem, we will change the queue bootstrapping as follows:
```cs
builder.AddAzureQueueClient("Queues", configureSettings: null, configureClientBuilder: config =>
{
    config.ConfigureOptions(configureOptions =>
    {
        configureOptions.MessageEncoding = QueueMessageEncoding.Base64;
    });
});
```

### Using the Queue
Using the queue, is straightforward. First, we have to inject the ```QueueServiceClient``` as follows:
```cs
@inject QueueServiceClient Queues
```

and then use the queue to send the message:
```cs
var queue = Queues.GetQueueClient("emails");
await queue.CreateIfNotExistsAsync();
var message = JsonSerializer.Serialize(_emailMessage);
await queue.SendMessageAsync(message);
```

That's it for the client

## The Function
Nothing fancy about the azure fuction. It will be a standard function with a queue trigger consuming the message:
```cs
[Function(nameof(EmailFunction))]
public void Run([QueueTrigger("emails")] EmailMessage message)
{
        logger.LogInformation("Sending an email to {To} with body {Body}", message.To, message.Body);
}
```

The only different thing, is that we will use the .NET Aspire [service defaults](/https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults) as follows:

```cs
var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();
```

We are done now with the function.

## The Host
The host should setup the following components:
- The storage
- The function app
- The client app

### The Storage
Azure storage integration is explained in details [here](https://learn.microsoft.com/en-us/dotnet/aspire/storage/azure-storage-blobs-integration?tabs=dotnet-cli). From the host point of view, storage is just a resource as any other resource.

The following code sets the storage up for the orchestration:
```cs
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
                     .RunAsEmulator();

var queues = storage.AddQueues("Queues");
```

The ```AddAzureStorage``` method adds the storage service to the orhechestration. This call: ```storage.AddQueues("Queues")``` means that we add a queue service explicitly that will provide the connection string through the key **Queues**.

The ```.RunAsEmulator()``` use the emulated version of azure storage instead of a concreate storage account. Of course, this code should not be used in production.

### The Function
Now, the host should add the azure function as a resource. Each resource can have a dependency, in our case, the function should wait for the queue to be running and use the storage for function storage.

```cs
builder
    .AddAzureFunctionsProject<EmailPublisherFunction>("EmailFunction")
    .WithExternalHttpEndpoints()
    .WithHostStorage(storage)
    .WaitFor(queues);
```

### The Client
The client is the last resource that we will start. It has a dependency on the queue.

```cs
builder
    .AddProject<Projects.AspireFunctionsClient>("EmailClient")
    .WithReference(queues)
    .WaitFor(queues);
```

# Execution
We have now everything we need to make it work. Let's see everything in action.
## The Aspire Dashboard
The resource list is displayed as follows:<br/>

![architecture](/images/resource-list.png)

We can see clearly the three resources that we are using (client, function and storage + queues).

The 9.2 of Aspire has a very nice addition which is the resource graph which an awesome way to illustrate the dependencies: <br/>

![graph](/images/resource-graph.png)

Aspire uses docker containers to run certain resources such as the azure emulator. If you run ```docker ps``` in your terminal, you can see the azure emulator container running:<br/>

![docker](/images/docker-ps.png)

## Running App
You can test it all together. The - very - ugly Blazor app just takes an email and a body and sends them to the queue.<br/>
![capture](/images/app-capture.png)

You can see in the function logs that it actually works! <br/>
![logs](/images/function-logs.png)