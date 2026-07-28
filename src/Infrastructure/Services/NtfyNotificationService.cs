// FILE: \src\Infrastructure\Services\NtfyNotificationService.cs
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SberAzsMonitoring.Application.Interfaces;
using SberAzsMonitoring.Application.Common.Configurations;

namespace SberAzsMonitoring.Infrastructure.Services;

public class NtfyNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NtfyNotificationService> _logger;
    private readonly string _ntfyTopicName;

    public NtfyNotificationService(
        HttpClient httpClient,
        ILogger<NtfyNotificationService> logger,
        IOptions<RegionOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Извлекаем имя топика региона (например, "fuel-snapshots-pskov")
        _ntfyTopicName = options.Value.KafkaTopicName;
    }
    public async Task SendPushNotificationAsync(
        string message,
        string title = "Мониторинг АЗС",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_ntfyTopicName))
        {
            _logger.LogError("Критическая ошибка: Имя топика ntfy не задано для данного региона!");
            return;
        }

        try
        {
            // ЧИСТАЯ АРХИТЕКТУРА: Забираем адрес и пароль напрямую из ОС перед отправкой
            string baseUrlStr = Environment.GetEnvironmentVariable("RegionSettings__NtfyBaseUrl") ?? "http://ntfy-server";
            // Регионы используют тот же пароль админа, что и воркер для пробива deny-all
            string adminPassword = Environment.GetEnvironmentVariable("NotificationWorkerOptions__NtfyAdminPassword") ?? "SecureAdminPassword2026!";

            var baseUrl = new Uri(baseUrlStr);
            var requestUri = new Uri(baseUrl, _ntfyTopicName); // Конструируем точный абсолютный URI

            _logger.LogInformation("Отправка Push-уведомления на локальный ntfy-server в топик: {Topic}...", _ntfyTopicName);

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(message, Encoding.UTF8, "text/plain")
            };

            var titleBytes = Encoding.UTF8.GetBytes(title);
            var base64Title = Convert.ToBase64String(titleBytes);
            var rfc2047Title = $"=?UTF-8?B?{base64Title}?=";

            request.Headers.Add("Title", rfc2047Title);
            request.Headers.Add("Priority", "5");
            request.Headers.Add("Tags", "warning,fuelpump");

            // ПРОБИВ BASIC AUTH: Кодируем admin:пароль в Base64 прямо в памяти
            string rawCredentials = $"admin:{adminPassword}";
            string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Credentials);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Push-уведомление успешно доставлено на локальный сервер.");
            }
            else
            {
                _logger.LogError("Локальный сервер ntfy вернул ошибку: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критический сбой сети при попытке отправить Push-уведомление на локальный сервер.");
        }
    }
}
