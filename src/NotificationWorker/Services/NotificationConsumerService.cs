using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SberAzsMonitoring.Application.Common.Contracts;
using SberAzsMonitoring.NotificationWorker.Application.UseCases;
using SberAzsMonitoring.NotificationWorker.Configurations;
using SberAzsMonitoring.NotificationWorker.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.NotificationWorker.Services;

public sealed class NotificationConsumerService : BackgroundService
{
    private readonly NotificationWorkerOptions _options;
    private readonly ILogger<NotificationConsumerService> _logger;
    private readonly ProcessFuelSnapshotUseCase _fuelSnapshotUseCase;
    private readonly ConsumerConfig _consumerConfig;
    private readonly Dictionary<string, string> _lastAlertStateCache = new(StringComparer.OrdinalIgnoreCase);

    // Потокобезопасный In-Memory кэш конфигураций активных фирм (Key: TenantId)
    private readonly ConcurrentDictionary<Guid, TenantConfigurationIntegrationEvent> _tenantsCache = new();

    public NotificationConsumerService(
        IOptions<NotificationWorkerOptions> options,
        ILogger<NotificationConsumerService> logger,
        ProcessFuelSnapshotUseCase fuelSnapshotUseCase)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fuelSnapshotUseCase = fuelSnapshotUseCase ?? throw new ArgumentNullException(nameof(fuelSnapshotUseCase));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.KafkaBootstrapServers))
            throw new InvalidOperationException("Критическая ошибка конфигурации Kafka: 'KafkaBootstrapServers' пуст.");
        if (string.IsNullOrWhiteSpace(_options.KafkaConsumerGroupId))
            throw new InvalidOperationException("Критическая ошибка конфигурации Kafka: 'KafkaConsumerGroupId' пуст.");
        if (string.IsNullOrWhiteSpace(_options.KafkaTopicsToListen))
            throw new InvalidOperationException("Критическая ошибка конфигурации Kafka: 'KafkaTopicsToListen' не задан.");

        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.KafkaBootstrapServers,
            GroupId = _options.KafkaConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true,
            TopicMetadataRefreshIntervalMs = 10000,
            AllowAutoCreateTopics = true
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Центральная служба уведомлений успешно запущена.");

        var topics = _options.KafkaTopicsToListen
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (!topics.Any())
        {
            _logger.LogCritical("Список топиков для прослушивания пуст. Служба останавливается.");
            return;
        }

        using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
        consumer.Subscribe(topics);
        _logger.LogInformation("Воркер успешно подписался на топики: {Topics}", string.Join(", ", topics));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult == null || consumeResult.IsPartitionEOF) continue;

                // 1. ПЕРЕХВАТ СОБЫТИЙ СИНХРОНИЗАЦИИ ФИРМ ИЗ ДАШБОРДА
                if (consumeResult.Topic.Equals("tenant-configuration-events", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var configEvent = JsonSerializer.Deserialize<TenantConfigurationIntegrationEvent>(
                            consumeResult.Message.Value,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (configEvent != null)
                        {
                            if (configEvent.IsDeleted)
                            {
                                _tenantsCache.TryRemove(configEvent.TenantId, out _);
                                _logger.LogInformation("[NotificationWorker] Фирма удалена из кэша распределения.");
                            }
                            else
                            {
                                _tenantsCache[configEvent.TenantId] = configEvent;
                                _logger.LogInformation(
                                    "[NotificationWorker] Конфигурация фирмы '{Name}' успешно синхронизирована. Активных каналов: {Count}",
                                    configEvent.Name,
                                    configEvent.Channels?.Count ?? 0);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[NotificationWorker] Ошибка десериализации события конфигурации тенанта.");
                    }
                    continue;
                }

                // 2. ОБРАБОТКА СРЕЗОВ ЦЕН АЗС ПОД РЕГИОНАЛЬНЫЙ ФОРМАТ
                var scanEvent = JsonSerializer.Deserialize<RegionScanIntegrationEvent>(consumeResult.Message.Value);
                if (scanEvent == null) continue;

                string regionName = !string.IsNullOrWhiteSpace(scanEvent.RegionName)
                    ? scanEvent.RegionName
                    : consumeResult.Topic;

                var currentSb = new StringBuilder();
                foreach (var st in scanEvent.Stations.OrderBy(s => s.Id))
                {
                    foreach (var f in st.Fuels.OrderBy(x => x.FuelType))
                    {
                        currentSb.Append($"{st.Id}:{f.FuelType}:{f.IsAvailable}:{f.LimitLiters};");
                    }
                }
                _lastAlertStateCache[regionName] = currentSb.ToString();

                var stationsWithGasoline = scanEvent.Stations
                    .Select(st => new {
                        st.Id,
                        st.Name,
                        st.Address,
                        AvailableGasoline = st.Fuels
                            .Where(f => f.IsAvailable &&
                                !f.FuelType.Contains("dt", StringComparison.OrdinalIgnoreCase) &&
                                !f.FuelType.Contains("diesel", StringComparison.OrdinalIgnoreCase) &&
                                !f.FuelType.Contains("gas", StringComparison.OrdinalIgnoreCase) &&
                                !f.FuelType.Contains("lpg", StringComparison.OrdinalIgnoreCase) &&
                                !f.FuelType.Contains("cng", StringComparison.OrdinalIgnoreCase)).OrderBy(f => f.FuelType).ToList()
                    })
                    .Where(st => st.AvailableGasoline.Any()).OrderBy(st => st.Id).ToList();

                var alertSb = new StringBuilder();
                //alertSb.AppendLine($" Доступность бензина на АЗС [{regionName}]");
                alertSb.AppendLine("============================");
                alertSb.AppendLine();

                string alertMessage = "";
                if (!stationsWithGasoline.Any())
                {
                    _logger.LogInformation("[NotificationWorker] В регионе {Region} тотальный дефицит бензина. Пуш отсечен на этапе фильтрации.", regionName);
                    alertSb.AppendLine("Ни на одной АЗС не зафиксировано наличие бензина.");
                }
                else
                {
                    int counter = 1;
                    foreach (var station in stationsWithGasoline)
                    {
                        alertSb.AppendLine($"{counter}. {station.Name}");
                        alertSb.AppendLine($"   Адрес: {station.Address}");
                        var fuelsLine = string.Join(", ", station.AvailableGasoline.Select(f => $"{f.FuelType.ToUpper()} (Лимит: {f.LimitLiters} л)"));
                        alertSb.AppendLine($"   В наличии: {fuelsLine}");
                        alertSb.AppendLine();
                        counter++;
                    }
                }
                alertMessage = alertSb.ToString();

                // =================================================================================
                // ПРОМЫШЛЕННАЯ ВЕЕРНАЯ РАССЫЛКА ПУШЕЙ (O(1) Оптимизация)
                // =================================================================================
                try
                {
                    // Имя топика ntfy берется напрямую из Kafka (например, fuel-snapshots-pskov)
                    string targetNtfyTopic = consumeResult.Topic;

                    // Публикуем срез один раз под учетной записью администратора.
                    // ntfy-server сам веерно раздаст его всем фирмам, у которых баланс > 0 (активные токены).
                    await _fuelSnapshotUseCase.ExecuteAsync(
                        regionName,
                        targetNtfyTopic,
                        alertMessage,
                        stoppingToken);

                    _logger.LogInformation(
                        "[NotificationWorker] Региональный пуш успешно опубликован в топик ntfy: {Topic}",
                        targetNtfyTopic);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[NotificationWorker] Критическая ошибка при публикации пуша в ntfy.");
                }
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                _logger.LogInformation("[NotificationWorker] Топики еще не инициализированы в Kafka. Ожидаем автосоздания...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в цикле службы уведомлений.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }
}
