using System;
using System.Collections.Generic;

namespace SberAzsMonitoring.NotificationWorker.Contracts;

/// <summary>
/// Интеграционное событие обновления конфигурации фирмы, поступающее из шины Kafka.
/// </summary>
public sealed class TenantConfigurationIntegrationEvent
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public bool IsDeleted { get; set; }
    public List<TenantChannelContract> Channels { get; set; } = new();
}

/// <summary>
/// Контракт канала привязки фирмы к региону АЗС.
/// </summary>
public sealed class TenantChannelContract
{
    public string RegionName { get; set; } = string.Empty;
    public string NtfyTopic { get; set; } = string.Empty;
}
