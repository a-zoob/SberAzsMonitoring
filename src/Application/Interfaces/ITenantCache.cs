using SberAzsMonitoring.NotificationWorker.Domain.Entities;

namespace SberAzsMonitoring.NotificationWorker.Application.Common.Interfaces;

/// <summary>
/// Интерфейс потокобезопасного хранилища конфигураций фирм в оперативной памяти воркера.
/// </summary>
public interface ITenantCache
{
    /// <summary>
    /// Добавляет или обновляет конфигурацию фирмы в кэше с проверкой актуальности по дате.
    /// </summary>
    void UpdateOrAdd(Tenant tenant);

    /// <summary>
    /// Удаляет фирму из кэша (например, если подписка аннулирована).
    /// </summary>
    void Remove(string tenantId);

    /// <summary>
    /// Возвращает список всех фирм, подписанных на определенный регион.
    /// </summary>
    IReadOnlyCollection<Tenant> GetTenantsSubscribedToRegion(string regionName);
}
