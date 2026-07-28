namespace SberAzsMonitoring.Domain;

public class FuelState
{
    public string FuelType { get; set; } = string.Empty; // "ai92", "ai95", "diesel", "propane"
    public bool IsAvailable { get; set; }
    public string AvailabilityStatus { get; set; } = string.Empty; // "stale", "available", "unknown"
    public int LimitLiters { get; set; } // 0 — если без лимитов, или конкретное число (30)
}
