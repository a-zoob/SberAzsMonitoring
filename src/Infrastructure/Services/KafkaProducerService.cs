using Confluent.Kafka;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SberAzsMonitoring.Application.Interfaces;
using SberAzsMonitoring.Application.Common.Contracts;
using SberAzsMonitoring.Application.Common.Configurations;

namespace SberAzsMonitoring.Infrastructure.Services;

public class KafkaProducerService : IKafkaProducerService
{
    private readonly IProducer<string, string> _producer;
    private readonly RegionOptions _options;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IOptions<RegionOptions> options, ILogger<KafkaProducerService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.KafkaBootstrapServers,
            ClientId = $"Producer-{_options.Name}"
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishRegionScanAsync(RegionScanIntegrationEvent scanEvent, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key = _options.Name,
            Value = JsonSerializer.Serialize(scanEvent)
        };

        try
        {
            // Брокер сам создаст топик _options.KafkaTopicName при этом вызове
            var result = await _producer.ProduceAsync(_options.KafkaTopicName, message, cancellationToken);
            _logger.LogInformation("Данные среза успешно отправлены в Kafka. Топик: {Topic}, Смещение: {Offset}",
                _options.KafkaTopicName, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Ошибка отправки сообщения в Kafka топик {Topic}: {Reason}", _options.KafkaTopicName, ex.Error.Reason);
            throw;
        }
    }
}
