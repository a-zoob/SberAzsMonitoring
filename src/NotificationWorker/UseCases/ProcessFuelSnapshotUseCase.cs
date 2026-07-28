using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SberAzsMonitoring.NotificationWorker.Application.UseCases;

/// <summary>
/// Бизнес-сценарий публикации среза цен на топливо в изолированные 
/// каналы ntfy от имени администратора.
/// </summary>
public sealed class ProcessFuelSnapshotUseCase
{
    private readonly ILogger<ProcessFuelSnapshotUseCase> _logger;
    private readonly HttpClient _httpClient;

    public ProcessFuelSnapshotUseCase(
        ILogger<ProcessFuelSnapshotUseCase> logger,
        HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Выполняет веерную публикацию пуш-уведомления для всех активных 
    /// подписчиков региона.
    /// </summary>
    /// <param name="regionName">Название региона (для формирования заголовка).</param>
    /// <param name="ntfyTopic">Целевой системный топик ntfy.</param>
    /// <param name="alertMessage">Текстовое сообщение со списком АЗС и ценами.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task ExecuteAsync(
        string regionName,
        string ntfyTopic,
        string alertMessage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[UseCase] Публикация веерного пуша для региона '{Region}' в топик '{Topic}'", regionName, ntfyTopic);

        string baseUrlStr = Environment.GetEnvironmentVariable("NotificationWorkerOptions__NtfyBaseUrl") ?? "http://ntfy-server";
        if (!baseUrlStr.EndsWith("/"))
        {
            baseUrlStr += "/";
        }

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(baseUrlStr);
        }

        string rawTitle = $" Изменение цен АЗС [{regionName}]";
        string base64Title = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawTitle));
        string encodedTitle = $"=?utf-8?B?{base64Title}?=";

        var request = new HttpRequestMessage(HttpMethod.Post, ntfyTopic)
        {
            Content = new StringContent(alertMessage, Encoding.UTF8, "text/plain")
        };

        request.Headers.TryAddWithoutValidation("Title", encodedTitle);
        request.Headers.TryAddWithoutValidation("Priority", "3");

        // ПРОВЕРКА НА ПУБЛИЧНЫЙ СЕРВЕР NTFY.SH
        // Если работаем с публичным сервером ntfy.sh — отправляем анонимно без авторизации
        if (baseUrlStr.Contains("ntfy.sh", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("[UseCase] Обнаружен публичный сервер ntfy.sh. Авторизация пропущена.");
        }
        else
        {
            // СБОРКА УЧЕТНЫХ ДАННЫХ ДЛЯ ЛОКАЛЬНОГО СЕРВЕРА
            string adminUser = Environment.GetEnvironmentVariable("NotificationWorkerOptions__NtfyAdminUser") ?? "admin";
            string adminPassword = Environment.GetEnvironmentVariable("NotificationWorkerOptions__NtfyAdminPassword") ?? "SecureAdminPassword2026!";
            string rawCredentials = $"{adminUser}:{adminPassword}";
            string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));

            request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Credentials}");
        }

        try
        {
            // Отправляем HTTP POST на целевой сервер ntfy
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[UseCase] Веерный пуш успешно принят ntfy для топика '{Topic}'", ntfyTopic);
                return;
            }
            // ТОТАЛЬНАЯ ДИАГНОСТИКА ПРИ ОШИБКЕ 401
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                string adminUser = Environment.GetEnvironmentVariable("NotificationWorkerOptions__NtfyAdminUser") ?? "admin";
                string adminPassword = Environment.GetEnvironmentVariable("NotificationWorkerOptions__NtfyAdminPassword") ?? "SecureAdminPassword2026!";
                string rawCredentials = $"{adminUser}:{adminPassword}";
                string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));

                _logger.LogCritical("========================================================================");
                _logger.LogCritical("[CRITICAL_DIAGNOSTIC] КРИТИЧЕСКИЙ СБОЙ АВТОРИЗАЦИИ 401!");
                _logger.LogCritical("[CRITICAL_DIAGNOSTIC] Считанный URL: '{Url}'", baseUrlStr);
                _logger.LogCritical("[CRITICAL_DIAGNOSTIC] Считанный Пользователь: '{User}'", adminUser);
                _logger.LogCritical("[CRITICAL_DIAGNOSTIC] Считанный Пароль: '{Pass}'", adminPassword);
                _logger.LogCritical("[CRITICAL_DIAGNOSTIC] Сформированная пара: '{Raw}'", rawCredentials);
                _logger.LogCritical("[CRITICAL_DIAGNOSTIC] Итоговый Base64 токен: 'Basic {B64}'", base64Credentials);

                bool hasAuthHeader = request.Headers.Contains("Authorization");
                _logger.LogCritical("[CRITICAL_DIAGNOSTIC] Наличие заголовка Authorization в HttpRequestMessage: {HasHeader}", hasAuthHeader);
                _logger.LogCritical("========================================================================");
            }

            _logger.LogError("[UseCase] Сервер ntfy отказал в публикации. Статус: {Code}", response.StatusCode);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UseCase] Критическая ошибка рантайма при отправке пуша.");
            throw;
        }
    }
}

