using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UglyToad.PdfPig;
using PdfHandler.Common;
using PdfHandler.Common.Contracts;
using PdfHandler.API.Data;

namespace PdfHandler.Worker;

public class PdfWorker : BackgroundService
{
    private readonly ILogger<PdfWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _hostname;

    public PdfWorker(ILogger<PdfWorker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostname = configuration["RabbitMq:Host"] ?? "localhost";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = _hostname };

        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: "pdf_tasks",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                var task = JsonSerializer.Deserialize<PdfTask>(message);
                if (task != null)
                {
                    await ProcessPdfAsync(task);
                }

                // Подтверждаем успешную обработку сообщения
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка обработки сообщения: {ex.Message}");
                // В случае тяжелой ошибки возвращаем в очередь (requeue: true)
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queue: "pdf_tasks", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        // Держим воркер активным, пока не отменят токен
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessPdfAsync(PdfTask task)
    {
        // Так как BackgroundService работает как Singleton, для DbContext (Scoped) нужен свой Scope
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var document = await dbContext.Documents.FindAsync(task.DocumentId);
        if (document == null) return;

        try
        {
            // 1. Обновляем статус на "В процессе"
            document.Status = ProcessingStatus.Processing;
            await dbContext.SaveChangesAsync();

            _logger.LogInformation($"Парсинг файла: {task.FilePath}");

            // 2. Извлекаем текст через PdfPig
            if (!File.Exists(task.FilePath))
            {
                throw new FileNotFoundException($"Файл не найден по пути: {task.FilePath}");
            }

            using var pdf = PdfDocument.Open(task.FilePath);
            var extractedText = string.Join(" ", pdf.GetPages().Select(p => p.Text));

            // 3. Сохраняем результат
            document.Content = extractedText;
            document.Status = ProcessingStatus.Completed;
            await dbContext.SaveChangesAsync();

            _logger.LogInformation($"Документ {task.DocumentId} успешно обработан.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка при парсинге PDF {task.DocumentId}: {ex.Message}");

            document.Status = ProcessingStatus.Failed;
            await dbContext.SaveChangesAsync();
        }
    }
}