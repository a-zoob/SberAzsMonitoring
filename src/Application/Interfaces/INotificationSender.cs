using SberAzsMonitoring.NotificationWorker.Domain.Entities;

namespace SberAzsMonitoring.NotificationWorker.Application.Common.Interfaces;

/// <summary>
/// Интерфейс службы отправки push-уведомлений во внешнюю систему.
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// Отправляет push-уведомление в конкретный топик ntfy с авторизацией конкретной фирмы.
    /// </summary>
    /// <param name="tenant">Сущность фирмы (содержит Access Token).</param>
    /// <param name="ntfyTopic">Целевой изолированный топик ntfy для данного региона.</param>
    /// <param name="title">Заголовок пуша (например, "Мониторинг АЗС — Псков").</param>
    /// <param name="message">Сформированный текстовый срез данных по топливу.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task<bool> SendAsync(
        Tenant tenant,
        string ntfyTopic,
        string title,
        string message,
        CancellationToken cancellationToken);
}
