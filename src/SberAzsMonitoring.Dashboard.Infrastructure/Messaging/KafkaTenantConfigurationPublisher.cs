using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Domain.Entities;

namespace SberAzsMonitoring.Dashboard.Infrastructure.Messaging;

public sealed class KafkaTenantConfigurationPublisher : ITenantConfigurationPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly IDataEncryptionService _encryptionService;
    private readonly ILogger<KafkaTenantConfigurationPublisher> _logger;
    private const string TopicName = "tenant-configuration-events";

    public KafkaTenantConfigurationPublisher(
        IDataEncryptionService encryptionService,
        ILogger<KafkaTenantConfigurationPublisher> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = "kafka:29092",
            Acks = Acks.All, // Гарантия enterprise-доставки: подтверждение от всех реплик Kafka
            MessageSendMaxRetries = 5
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishUpdateAsync(DashboardTenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        // 1. Расшифровываем токен для передачи в доверенную среду Воркера Уведомлений
        string? cleanToken = !string.IsNullOrEmpty(tenant.EncryptedNtfyAccessWithValue)
            ? _encryptionService.Decrypt(tenant.EncryptedNtfyAccessWithValue)
            : null;

        // 2. Преобразуем доменную коллекцию каналов (Вариант Б) в плоский словарь для контракта Kafka
        var regionChannelsDict = tenant.Channels
            .ToDictionary(c => c.RegionName, c => c.NtfyTopic, StringComparer.OrdinalIgnoreCase);

        // 3. Формируем DTO, полностью идентичный структуре десериализации в Воркере Уведомлений
        var eventDto = new
        {
            TenantId = tenant.Id.ToString(),
            Name = tenant.Name,
            AccessToken = cleanToken,
            RegionChannels = regionChannelsDict,
            UpdatedAt = tenant.UpdatedAt,
            IsDeleted = tenant.IsDeleted
        };

        string jsonPayload = JsonSerializer.Serialize(eventDto);

        // 4. Отправляем сообщение. Ключом является Id фирмы для обеспечения порядка (Order Guarantees) в рамках одной партиции
        var message = new Message<string, string>
        {
            Key = tenant.Id.ToString(),
            Value = jsonPayload
        };

        try
        {
            var deliveryResult = await _producer.ProduceAsync(TopicName, message, cancellationToken);
            _logger.LogInformation("Событие конфигурации фирмы {TenantId} успешно отправлено в Kafka (Патиция: {Partition}, Смещение: {Offset})",
                tenant.Id, deliveryResult.Partition, deliveryResult.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Критический сбой отправки конфигурации фирмы {TenantId} в Kafka.", tenant.Id);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
