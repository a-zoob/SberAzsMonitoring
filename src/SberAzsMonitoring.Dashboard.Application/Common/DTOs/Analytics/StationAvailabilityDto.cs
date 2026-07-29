namespace SberAzsMonitoring.Dashboard.Application.Common.DTOs.Analytics;

public record StationAvailabilityDto(
    string Region,
    string StationId,
    string StationName,
    string StationAddress,
    string FuelType,
    bool IsAvailable,
    string AvailabilityStatus,
    int LimitLiters,
    DateTime Timestamp
);