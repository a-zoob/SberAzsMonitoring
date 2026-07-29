using SberAzsMonitoring.Dashboard.Application.Common.Interfaces.Analytics;

namespace SberAzsMonitoring.Dashboard.Application.UseCases.Analytics;

public class GetAvailableRegionsUseCase
{
    private readonly IClickHouseAnalyticsRepository _analyticsRepository;

    public GetAvailableRegionsUseCase(IClickHouseAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<IEnumerable<string>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await _analyticsRepository.GetAvailableRegionsAsync(cancellationToken);
    }
}
