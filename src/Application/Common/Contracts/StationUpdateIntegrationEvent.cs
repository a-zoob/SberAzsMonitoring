namespace SberAzsMonitoring.Application.Common.Contracts;

// Контракт содержит сразу весь массив заправок, полученных при сканировании
public record RegionScanIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string RegionName { get; init; } = null!;
    /// <summary>
    /// Маркер фирмы-инициатора ручного пуша. Если null — это плановое сканирование.
    /// </summary>
    public string? TargetTenantId { get; init; }
    public List<FuelStationContract> Stations { get; init; } = new();

}

public record FuelStationContract
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public List<FuelStateContract> Fuels { get; init; } = new();
}
