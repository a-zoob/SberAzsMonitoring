using SberAzsMonitoring.Dashboard.Application.Common.DTOs.Analytics;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces.Analytics;

namespace SberAzsMonitoring.Dashboard.Application.UseCases.Analytics;

public class GetLatestFuelAvailabilityUseCase
{
    private readonly IClickHouseAnalyticsRepository _analyticsRepository;

    public GetLatestFuelAvailabilityUseCase(IClickHouseAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<IEnumerable<StationAvailabilityDto>> ExecuteAsync(
        IEnumerable<string>? regions = null,
        CancellationToken cancellationToken = default)
    {
        return await _analyticsRepository.GetCurrentRegistryAsync(regions, cancellationToken);
    }
}
