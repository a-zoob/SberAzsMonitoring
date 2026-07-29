using SberAzsMonitoring.Dashboard.Application.Common.DTOs.Analytics;

namespace SberAzsMonitoring.Dashboard.Application.Common.Interfaces.Analytics;

public interface IClickHouseAnalyticsRepository
{
    // Получить последний актуальный срез по АЗС
    Task<IEnumerable<StationAvailabilityDto>> GetCurrentRegistryAsync(
        IEnumerable<string>? regions = null,
        CancellationToken cancellationToken = default);

    // Получить список всех уникальных регионов, присутствующих в базе данных
    Task<IEnumerable<string>> GetAvailableRegionsAsync(CancellationToken cancellationToken = default);

    // Получить список всех уникальных марок бензина, присутствующих в базе данных
    Task<IEnumerable<string>> GetAvailableFuelTypesAsync(CancellationToken cancellationToken = default);
}
