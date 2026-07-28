using SberAzsMonitoring.Application.Interfaces;
using SberAzsMonitoring.Application.Common.DTOs;

namespace SberAzsMonitoring.Infrastructure.Repositories;

public class FuelRepository : IFuelRepository
{
    public Task<IEnumerable<FuelStationDto>> GetCurrentStationsAsync(CancellationToken cancellationToken)
    {
        // WebApi региона больше не имеет прямого доступа к СУБД
        return Task.FromResult(Enumerable.Empty<FuelStationDto>());
    }

    public Task<IEnumerable<FuelStateDto>> GetPriceHistoryAsync(string stationId, string fuelType, CancellationToken cancellationToken)
    {
        return Task.FromResult(Enumerable.Empty<FuelStateDto>());
    }

    public Task SaveRegionScanSnapshotAsync(List<FuelStationDto> stations, CancellationToken cancellationToken)
    {
        return Task.CompletedTask; // Локально ничего не пишем, всё улетает через KafkaProducer
    }

    public Task SaveStationSnapshotAsync(FuelStationDto station, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
