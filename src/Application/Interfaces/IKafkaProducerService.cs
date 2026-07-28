using SberAzsMonitoring.Application.Common.Contracts;

namespace SberAzsMonitoring.Application.Interfaces;

public interface IKafkaProducerService
{
    Task PublishRegionScanAsync(RegionScanIntegrationEvent scanEvent, CancellationToken cancellationToken = default);
}

