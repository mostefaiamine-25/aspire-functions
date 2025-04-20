using AspireFunctions.Contracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmailPublisherFunction;

public class EmailFunction(ILogger<EmailFunction> logger)
{   

    [Function(nameof(EmailFunction))]
    public void Run([QueueTrigger("emails")] EmailMessage message)
    {
        logger.LogInformation("Sending an email to {To} with body {Body}", message.To, message.Body);
    }
}
