using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace PdfHandler.API.Services;

public class QueueService
{
    private readonly IConfiguration _configuration;
    private readonly string _hostname;

    public QueueService(IConfiguration configuration)
    {
        _configuration = configuration;
        _hostname = _configuration["RabbitMq:Host"] ?? "localhost";
    }

    public async Task PublishPdfTaskAsync(Guid documentId, string filePath)
    {
        var factory = new ConnectionFactory { HostName = _hostname };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "pdf_tasks",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var taskMessage = JsonSerializer.Serialize(new { DocumentId = documentId, FilePath = filePath });
        var body = Encoding.UTF8.GetBytes(taskMessage);

        var properties = new BasicProperties
        {
            Persistent = true
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "pdf_tasks",
            mandatory: false,
            basicProperties: properties,
            body: body
        );
    }
};