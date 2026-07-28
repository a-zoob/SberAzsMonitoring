using SberAzsMonitoring.Dashboard.Domain.Entities;

namespace SberAzsMonitoring.Dashboard.Application.Common.Interfaces;

/// <summary>
/// Интерфейс службы публикации изменений конфигурации фирм в брокер сообщений.
/// </summary>
public interface ITenantConfigurationPublisher
{
    /// <summary>
    /// Публикует событие обновления или удаления настроек фирмы в топик Kafka.
    /// </summary>
    /// <param name="tenant">Сущность фирмы с актуальными данными.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task PublishUpdateAsync(DashboardTenant tenant, CancellationToken cancellationToken = default);
}
