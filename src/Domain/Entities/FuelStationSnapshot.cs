namespace SberAzsMonitoring.Domain.Entities;

public class FuelStationSnapshot
{
    public string Region { get; set; } = string.Empty;
    public string StationId { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public string StationAddress { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public byte IsAvailable { get; set; }
    public string AvailabilityStatus { get; set; } = string.Empty;
    public int LimitLiters { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid EventId { get; set; }
}