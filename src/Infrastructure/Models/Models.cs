namespace SberAzsMonitoring.Infrastructure.Models;

// Корневой объект ответа API sberazs.ru
internal record SberAzsApiResponse(
    List<StationJsonModel> Stations
);

// Модель заправки со скриншота
internal record StationJsonModel(
    string Id,
    string Name,
    string Address,
    LocationJsonModel Location,
    List<FuelJsonModel> Fuels
);

// Модель координат (учитываем точный ключ "lon")
internal record LocationJsonModel(
    double Lat,
    double Lon
);

// Модель топлива с новыми критическими параметрами
internal record FuelJsonModel(
    string Type,                  // "ai92", "ai95" и т.д.
    string AvailabilityStatus,    // "available", "stale", "unknown"
    int? LimitLiters              // Лимит литров, если есть
);
