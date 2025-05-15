using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using fnPayment.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace fnPayment;

public class Payment
{
    private readonly ILogger<Payment> _logger;
    private readonly IConfiguration _configuration;
    private readonly string[] StatusList = { "Approved", "Declined", "Pending" };
    private readonly Random random = new Random();

    public Payment(ILogger<Payment> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function(nameof(Payment))]
    [CosmosDBOutput("%CosmoDB%", "%CosmosContainer%", Connection = "CosmosDBConnection", CreateIfNotExists = true )]

    public async Task<object?> Run(
        [ServiceBusTrigger("payment-queue", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {

        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);


        PaymentModel payment = null;

        try
        {
            payment = JsonSerializer.Deserialize<PaymentModel>(message.Body.ToString(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payment == null)
            {
                await messageActions.DeadLetterMessageAsync(message, null, "The message is not a valid PaymentModel.");
            }

            int index = random.Next(StatusList.Length);
            string status = StatusList[index];
            payment.Status = status;

            if (status == "Approved")
            {
                payment.DateApproval = DateTime.UtcNow;
                await SendToNotificationQueue(payment);

            }

            return payment;

        }
        catch (Exception ex)
        {
            await messageActions.DeadLetterMessageAsync(message, null, $"Error:{ex.Message}");
            return null;
        }
        finally
        {
            await messageActions.CompleteMessageAsync(message);
        }
        // Complete the message
        
    }

    private async Task SendToNotificationQueue(PaymentModel payment)
    {
        var connectionString = _configuration.GetSection("ServiceBusConnection").Value.ToString();
        var queueName = _configuration.GetSection("NotificationQueue").Value.ToString();

        var serviceBusClient = new ServiceBusClient(connectionString);
        var sender = serviceBusClient.CreateSender(queueName);
        var message = new ServiceBusMessage(JsonSerializer.Serialize(payment))
        {
            ContentType = "application/json",
            MessageId = payment.idPayment,

        };

        message.ApplicationProperties["IdPayment"] = payment.idPayment;
        message.ApplicationProperties["type"] = "notification";
        message.ApplicationProperties["message"] = "Payment Approved";

        try
        {
            await sender.SendMessageAsync(message);
            _logger.LogInformation("Message sent to notification queue: {messageId}", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to notification queue: {messageId}", message.MessageId);
        }
        finally
        {
            await sender.DisposeAsync();
            await serviceBusClient.DisposeAsync();
        }
    }
}