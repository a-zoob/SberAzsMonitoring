namespace SberAzsMonitoring.Domain;

public class FuelStation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<FuelState> FuelStates { get; set; } = new(); // Связываем с обновленным состоянием топлива
    public DateTime LastUpdatedAt { get; set; }
}
