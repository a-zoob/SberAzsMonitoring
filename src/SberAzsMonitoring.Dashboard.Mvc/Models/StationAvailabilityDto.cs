namespace SberAzsMonitoring.Dashboard.Mvc.Models
{
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
}
