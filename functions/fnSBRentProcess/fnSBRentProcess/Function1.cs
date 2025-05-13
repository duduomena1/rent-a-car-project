using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace fnSBRentProcess;

public class RentProcessFunction
{
    private readonly ILogger<RentProcessFunction> _logger;
    private readonly IConfiguration _configuration;

    public RentProcessFunction(ILogger<RentProcessFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function(nameof(RentProcessFunction))]
    public async Task Run(
        [ServiceBusTrigger("queue-locacao", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        var body = message.Body.ToString();
        _logger.LogInformation("Message Body: {body}", body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        RentModel rentModel = null;
        try
        {
            rentModel = JsonSerializer.Deserialize<RentModel>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (rentModel is null)
            {
                _logger.LogError("Deserialized object is null. Message body: {body}", body);
                await messageActions.DeadLetterMessageAsync(message, null, "Failed to deserialize message body.");
                return;
            }

            var connectionString = _configuration.GetConnectionString("SqlConnectionString");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var command =
                new SqlCommand(@"INSERT INTO Rent (Name, Mail, Model, Year, RentTime, Date)
                                        VALUES (@Name, @Mail, @Model, @Year, @RentTime, @Date)", connection);
            command.Parameters.AddWithValue("@Name", rentModel.Name);
            command.Parameters.AddWithValue("@Mail", rentModel.Mail);
            command.Parameters.AddWithValue("@Model", rentModel.Model);
            command.Parameters.AddWithValue("@Year", rentModel.Year);
            command.Parameters.AddWithValue("@RentTime", rentModel.RentTime);
            command.Parameters.AddWithValue("@Date", rentModel.Date);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            await messageActions.CompleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing message body: {body}", body);
            await messageActions.DeadLetterMessageAsync(message, null, "Failed to deserialize message body.");
            return;
        }

    }
}