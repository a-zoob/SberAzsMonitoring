using SberAzsMonitoring.NotificationWorker.Application.Common.Interfaces;
using SberAzsMonitoring.NotificationWorker.Domain.Entities;

namespace SberAzsMonitoring.NotificationWorker.Application.UseCases;

public sealed class UpdateTenantConfigUseCase
{
    private readonly ITenantCache _tenantCache;

    public UpdateTenantConfigUseCase(ITenantCache tenantCache)
    {
        _tenantCache = tenantCache;
    }

    /// <summary>
    /// Выполняет бизнес-логику обновления или удаления настроек фирмы в системе.
    /// </summary>
    public void Execute(
        string tenantId,
        string name,
        string? accessToken,
        Dictionary<string, string>? regionChannels,
        DateTime updatedAt,
        bool isDeleted)
    {
        if (isDeleted)
        {
            _tenantCache.Remove(tenantId);
            return;
        }

        // Инкапсулируем создание доменной сущности с валидацией внутренних правил
        var tenant = new Tenant(
            tenantId,
            name,
            accessToken ?? string.Empty,
            regionChannels ?? new(),
            updatedAt);

        _tenantCache.UpdateOrAdd(tenant);
    }
}
