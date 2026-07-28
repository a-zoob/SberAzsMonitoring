using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SberAzsMonitoring.NotificationWorker.Application.UseCases;
using System.Text.Json;

namespace SberAzsMonitoring.NotificationWorker.Infrastructure.Messaging;

public sealed class TenantConfigurationConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly UpdateTenantConfigUseCase _useCase;
    private readonly ILogger<TenantConfigurationConsumer> _logger;

    public TenantConfigurationConsumer(
        UpdateTenantConfigUseCase useCase,
        ILogger<TenantConfigurationConsumer> logger)
    {
        _useCase = useCase;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = "kafka:29092",
            GroupId = "sberazs-tenant-config-group",
            AutoOffsetReset = AutoOffsetReset.Earliest, // Читаем конфигурацию с начала времен при холодном старте
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("tenant-configuration-events");
        _logger.LogInformation("Запущен воркер динамической конфигурации фирм...");

        await Task.Run(() =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    if (consumeResult == null) continue;

                    // Парсим DTO события конфигурации из Дашборда
                    var evt = JsonSerializer.Deserialize<TenantConfigUpdatedEventDto>(consumeResult.Message.Value);
                    if (evt == null) continue;

                    // Вызываем сценарий чистой архитектуры
                    _useCase.Execute(evt.TenantId, evt.Name, evt.AccessToken, evt.RegionChannels, evt.UpdatedAt, evt.IsDeleted);

                    _logger.LogInformation("Конфигурация фирмы '{TenantId}' успешно обновлена в памяти на лету.", evt.TenantId);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке события конфигурации фирмы.");
                }
            }
        }, stoppingToken);
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}

// Простой DTO для десериализации
public record TenantConfigUpdatedEventDto(
    string TenantId, string Name, string? AccessToken,
    Dictionary<string, string>? RegionChannels, DateTime UpdatedAt, bool IsDeleted);
