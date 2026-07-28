namespace SberAzsMonitoring.Application.Common.DTOs;

public record FuelStateDto(string FuelType, bool IsAvailable, string AvailabilityStatus, int LimitLiters);
