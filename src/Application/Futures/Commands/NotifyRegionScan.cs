namespace SberAzsMonitoring.Application.Features.Commands;

/// <summary>
/// Команда на запуск сканирования региона и отправку уведомлений.
/// </summary>
public sealed class NotifyRegionScanCommand
{
    /// <summary>
    /// Идентификатор конкретной фирмы, запросившей ручной пуш. 
    /// Если null — это плановое сканирование для всех фирм региона.
    /// </summary>
    public string? TargetTenantId { get; }

    // Конструктор по умолчанию для плановых запусков (без аргументов)
    public NotifyRegionScanCommand()
    {
        TargetTenantId = null;
    }

    // Конструктор с параметром для ручного пуша из Дашборда
    public NotifyRegionScanCommand(string? targetTenantId)
    {
        TargetTenantId = targetTenantId;
    }
}
