using System;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.Dashboard.Application.Interfaces;

/// <summary>
/// Интерфейс службы авторизации ntfy для управления пользователями и правами.
/// </summary>
public interface INtfyAuthService
{
    /// <summary>
    /// Первичная регистрация фирмы в ntfy-server со стабильным паролем.
    /// </summary>
    Task<string> RegisterUserAsync(string tenantSystemLogin, CancellationToken cancellationToken);

    /// <summary>
    /// Динамическая выдача прав read-only на указанный системный топик.
    /// </summary>
    Task GrantAccessAsync(string tenantSystemLogin, string topicName, CancellationToken cancellationToken);

    Task RevokeAccessAsync(string username, string topic, CancellationToken cancellationToken = default);

    [Obsolete("Используйте раздельные методы RegisterUserAsync и GrantAccessAsync для поддержки N-подписок.")]
    Task<string> CreateSubscriptionTokenAsync(string tenantId, string regionName, string topicName, CancellationToken cancellationToken = default);
}
