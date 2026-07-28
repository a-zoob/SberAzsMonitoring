using SberAzsMonitoring.Application.Common.DTOs;

namespace SberAzsMonitoring.Application.Interfaces;

public interface IFuelRepository
{
    // Пакетное сохранение всего среза региона за один проход
    Task SaveRegionScanSnapshotAsync(List<FuelStationDto> stations, CancellationToken cancellationToken = default);

    Task SaveStationSnapshotAsync(FuelStationDto stationDto, CancellationToken cancellationToken = default);
    Task<IEnumerable<FuelStationDto>> GetCurrentStationsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<FuelStateDto>> GetPriceHistoryAsync(string stationId, string fuelType, CancellationToken cancellationToken = default);
}
