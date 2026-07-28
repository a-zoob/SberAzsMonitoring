using Confluent.Kafka;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SberAzsMonitoring.Application.Common.Contracts;
using SberAzsMonitoring.ClickHouseWorker.Configurations;
using SberAzsMonitoring.ClickHouseWorker.Data;
using SberAzsMonitoring.Domain.Entities;

namespace SberAzsMonitoring.ClickHouseWorker.Services;

public class ClickHouseConsumerService : BackgroundService
{
    private readonly WorkerOptions _options;
    private readonly ILogger<ClickHouseConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConsumerConfig _consumerConfig;
    private readonly List<string> _configuredTopics;

    public ClickHouseConsumerService(
        IOptions<WorkerOptions> options,
        ILogger<ClickHouseConsumerService> logger,
        IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;

        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.KafkaBootstrapServers,
            GroupId = _options.KafkaConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = false
        };

        _configuredTopics = _options.KafkaTopicsToListen
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuredTopics.Any())
        {
            _logger.LogCritical("Воркеру не переданы топики для прослушивания!");
            return;
        }

        _logger.LogInformation("Центральный воркер ClickHouse запущен. Подписка на топики: {Topics}", string.Join(", ", _configuredTopics));

        using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
        consumer.Subscribe(_configuredTopics);

        var batchBuffer = new List<(string Topic, string RegionKey, RegionScanIntegrationEvent Event)>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));

                if (consumeResult == null)
                {
                    if (batchBuffer.Any())
                    {
                        await FlushBufferAsync(batchBuffer, consumer, consumeResult, stoppingToken);
                    }
                    continue;
                }

                var scanEvent = JsonSerializer.Deserialize<RegionScanIntegrationEvent>(consumeResult.Message.Value);
                if (scanEvent != null)
                {
                    batchBuffer.Add((consumeResult.Topic, consumeResult.Message.Key ?? "Неизвестный регион", scanEvent));
                }

                if (batchBuffer.Count >= 1)
                {
                    await FlushBufferAsync(batchBuffer, consumer, consumeResult, stoppingToken);
                }
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                _logger.LogDebug("Один из топиков (Новгород) еще не создан на брокере. Ожидание...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в цикле центрального консьюмера ClickHouse.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }

    private async Task FlushBufferAsync(
        List<(string Topic, string RegionKey, RegionScanIntegrationEvent Event)> buffer,
        IConsumer<string, string> consumer,
        ConsumeResult<string, string>? lastResult,
        CancellationToken cancellationToken)
    {
        // Использование приватного логгера класса _logger внутри метода
        _logger.LogInformation("[SharedWorker] Начинается пакетный сброс {Count} срезов данных в ClickHouse...", buffer.Count);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            var rawEntities = new List<FuelStationSnapshot>();

            foreach (var item in buffer)
            {
                foreach (var station in item.Event.Stations)
                {
                    foreach (var fuel in station.Fuels)
                    {
                        rawEntities.Add(new FuelStationSnapshot
                        {
                            Region = item.RegionKey,
                            StationId = station.Id,
                            StationName = station.Name,
                            StationAddress = station.Address,
                            Latitude = station.Latitude,
                            Longitude = station.Longitude,
                            FuelType = fuel.FuelType,
                            IsAvailable = (byte)(fuel.IsAvailable ? 1 : 0),
                            AvailabilityStatus = fuel.AvailabilityStatus,
                            LimitLiters = fuel.LimitLiters,
                            Timestamp = item.Event.OccurredOn,
                            EventId = item.Event.EventId
                        });
                    }
                }
            }

            // Защита от Identity Conflict: убираем дубликаты по составному первичному ключу ClickHouse перед вставкой
            var uniqueEntitiesToInsert = rawEntities
                .GroupBy(e => new { e.Region, e.StationId, e.FuelType, e.Timestamp })
                .Select(g => g.First())
                .ToList();

            await dbContext.FuelStationSnapshots.AddRangeAsync(uniqueEntitiesToInsert, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (lastResult != null)
            {
                consumer.Commit(lastResult);
            }

            _logger.LogInformation("[SharedWorker] Успешно сохранено пачкой {Count} уникальных записей в ClickHouse, смещение зафиксировано.", uniqueEntitiesToInsert.Count);
            buffer.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка пакетного сохранения воркером в ClickHouse.");
            throw;
        }
    }
}
