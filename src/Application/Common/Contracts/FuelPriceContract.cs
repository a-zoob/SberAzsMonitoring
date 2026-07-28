namespace SberAzsMonitoring.Application.Common.Contracts;

public record FuelStateContract
{
    public string FuelType { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public string AvailabilityStatus { get; init; } = string.Empty;
    public int LimitLiters { get; init; }
}
