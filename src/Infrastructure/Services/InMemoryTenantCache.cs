using System.Collections.Concurrent;
using SberAzsMonitoring.NotificationWorker.Application.Common.Interfaces;
using SberAzsMonitoring.NotificationWorker.Domain.Entities;

namespace SberAzsMonitoring.NotificationWorker.Infrastructure.Cache;

/// <summary>
/// реализация кэша тенантов в оперативной памяти.
/// </summary>
public sealed class InMemoryTenantCache : ITenantCache
{
    // Хранилище: Ключ — Id фирмы, Значение — Сущность Tenant со всеми настройками
    private readonly ConcurrentDictionary<string, Tenant> _tenants = new();

    public void UpdateOrAdd(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        // Используем атомарный метод AddOrUpdate для потокобезопасного обновления
        _tenants.AddOrUpdate(
            tenant.Id,
            addValueFactory: _ => tenant,
            updateValueFactory: (id, existingTenant) =>
            {
                // Защита от Out-of-Order событий: обновляем только если новые данные действительно свежее
                if (tenant.OffsetUpdatedAt > existingTenant.OffsetUpdatedAt)
                {
                    return tenant;
                }

                // Иначе оставляем существующую, более актуальную версию
                return existingTenant;
            });
    }

    public void Remove(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) return;

        // Потокобезопасное удаление фирмы из кэша
        _tenants.TryRemove(tenantId, out _);
    }

    public IReadOnlyCollection<Tenant> GetTenantsSubscribedToRegion(string regionName)
    {
        if (string.IsNullOrWhiteSpace(regionName))
        {
            return Array.Empty<Tenant>();
        }

        // Фильтруем тенантов, у которых в доменной модели прописан данный регион.
        // Вызов ToList() делает потокобезопасный моментальный снимок (Snapshot) данных для итерации.
        return _tenants.Values
            .Where(t => t.IsSubscribedToRegion(regionName))
            .ToList();
    }
}
