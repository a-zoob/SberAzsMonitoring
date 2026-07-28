using System;

namespace SberAzsMonitoring.Dashboard.Domain.Entities;

/// <summary>
/// Сущность карты каналов Дашборда: конкретный регион -> конкретный топик ntfy.
/// </summary>
public sealed class DashboardTenantChannel
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string RegionName { get; private set; } = null!;
    public string NtfyTopic { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Конструктор для ORM (Entity Framework Core)
    private DashboardTenantChannel() { }

    public DashboardTenantChannel(Guid tenantId, string regionName, string ntfyTopic)
    {
        if (string.IsNullOrWhiteSpace(regionName))
            throw new ArgumentException("Имя региона не может быть пустым", nameof(regionName));
        if (string.IsNullOrWhiteSpace(ntfyTopic))
            throw new ArgumentException("Топик ntfy не может быть пустым", nameof(ntfyTopic));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        RegionName = regionName;
        NtfyTopic = ntfyTopic;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Бизнес-метод изменения целевого топика ntfy для региона.
    /// </summary>
    public void ChangeTopic(string newTopic)
    {
        if (string.IsNullOrWhiteSpace(newTopic))
            throw new ArgumentException("Топик ntfy не может быть пустым", nameof(newTopic));

        NtfyTopic = newTopic;
        UpdatedAt = DateTime.UtcNow;
    }
}
