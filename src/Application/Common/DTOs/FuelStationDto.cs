namespace SberAzsMonitoring.Application.Common.DTOs;

public record FuelStationDto(
    string Id, string Name, string Address,
    double Latitude, double Longitude,
    List<FuelStateDto> Fuels, DateTime UpdatedAt
);
