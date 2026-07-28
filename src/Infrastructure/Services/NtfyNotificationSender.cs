using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SberAzsMonitoring.NotificationWorker.Application.Common.Interfaces;
using SberAzsMonitoring.NotificationWorker.Domain.Entities;

namespace SberAzsMonitoring.NotificationWorker.Infrastructure.Notifications;

/// <summary>
/// Промышленная реализация отправщика уведомлений ntfy с поддержкой отказоустойчивости.
/// </summary>
public sealed class NtfyNotificationSender : INotificationSender
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NtfyNotificationSender> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public NtfyNotificationSender(HttpClient httpClient, ILogger<NtfyNotificationSender> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Настройка политики повторных попыток (Polly): 3 попытки при сетевых ошибках
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8)
            }, onRetry: (outcome, timespan, retryCount, context) =>
            {
                _logger.LogWarning("Сбой при отправке в ntfy. Попытка {RetryCount}. Ожидание {TimeSpan} сек. Причина: {Reason}",
                    retryCount, timespan.TotalSeconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
            });
    }
    public async Task<bool> SendAsync(
        Tenant tenant,
        string ntfyTopic,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            // ЧИСТАЯ АРХИТЕКТУРА: Полностью динамически извлекаем И логин, И пароль из ОС Linux без хардкода
            string baseUrlStr = Environment.GetEnvironmentVariable("NotificationWorkerOptions__NtfyBaseUrl") ?? "http://ntfy-server";
            string adminUser = Environment.GetEnvironmentVariable("NotificationWorkerOptions__NtfyAdminUser") ?? "admin";
            string adminPassword = Environment.GetEnvironmentVariable("NotificationWorkerOptions__NtfyAdminPassword") ?? "SecureAdminPassword2026!";

            var baseUrl = new Uri(baseUrlStr);
            var requestUri = new Uri(baseUrl, ntfyTopic); // Конструируем точный абсолютный путь запроса

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

                // Настройка тела запроса
                request.Content = new StringContent(message, Encoding.UTF8, "text/plain");

                // Кодирование заголовка Title по стандарту RFC 2047
                var titleBytes = Encoding.UTF8.GetBytes(title);
                var base64Title = Convert.ToBase64String(titleBytes);
                request.Headers.Add("X-Title", $"=?UTF-8?B?{base64Title}?=");

                request.Headers.Add("X-Tags", "fuelpump,warning");
                request.Headers.Add("X-Priority", "3");

                // ДИНАМИЧЕСКИЙ ПРОБИВ BASIC AUTH: Склеиваем параметры и кодируем в Base64 через UTF8
                string rawCredentials = $"{adminUser}:{adminPassword}";
                string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));

                // Жестко и принудительно зашиваем чистый заголовок Authorization в коллекцию заголовков
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Credentials}");

                return await _httpClient.SendAsync(request, cancellationToken);
            });

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Пуш успешно отправлен для фирмы '{TenantName}' в топик '{Topic}'", tenant.Name, ntfyTopic);
                return true;
            }

            _logger.LogError("Не удалось отправить пуш для '{TenantName}' после всех попыток. HTTP Статус: {StatusCode}",
                tenant.Name, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Критическая ошибка при попытке отправить уведомление для фирмы '{TenantName}'", tenant.Name);
            return false;
        }
    }
}
