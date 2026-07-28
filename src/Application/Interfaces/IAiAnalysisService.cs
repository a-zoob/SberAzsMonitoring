using SberAzsMonitoring.Application.Common.DTOs;

namespace SberAzsMonitoring.Application.Interfaces;

public interface IAiAnalysisService
{
    Task<AnalysisResultDto> AnalyzeRegionSupplyAsync(List<FuelStationDto> stations, CancellationToken cancellationToken = default);
}
