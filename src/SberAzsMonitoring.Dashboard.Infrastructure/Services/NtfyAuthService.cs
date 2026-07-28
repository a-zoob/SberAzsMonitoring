//using System;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Net.Http.Json;
//using System.Text;
//using System.Text.Json.Serialization;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using SberAzsMonitoring.Dashboard.Application.Interfaces;

//namespace SberAzsMonitoring.Dashboard.Infrastructure.Services;

///// <summary>
///// Промышленная реализация службы авторизации ntfy через административный HTTP API.
///// </summary>
//public sealed class NtfyAuthService : INtfyAuthService
//{
//    private readonly HttpClient _httpClient;
//    private readonly ILogger<NtfyAuthService> _logger;

//    public NtfyAuthService(HttpClient httpClient, ILogger<NtfyAuthService> logger)
//    {
//        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
//        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//    }

//    public async Task<string> CreateSubscriptionTokenAsync(
//     string tenantId,
//     string regionName,
//     string topicName,
//     CancellationToken cancellationToken = default)
//    {
//        // Извлекаем параметры конфигурации напрямую из ОС без хардкода
//        string baseUrlStr = Environment.GetEnvironmentVariable("RegionSettings__NtfyBaseUrl") ?? "http://ntfy-server";
//        string adminPassword = Environment.GetEnvironmentVariable("NotificationWorkerOptions__NtfyAdminPassword") ?? "SecureAdminPassword2026!";

//        // Так как мы передаем tenant.SystemLogin в параметр tenantId, 
//        // мы используем его напрямую как готовый логин для ntfy
//        string ntfyUsername = tenantId;

//        var baseUrl = new Uri(baseUrlStr);
//        string rawCredentials = $"admin:{adminPassword}";
//        string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));

//        _httpClient.BaseAddress = baseUrl;
//        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

//        try
//        {
//            _logger.LogInformation("[NtfyAuth] Инициализация подписки для пользователя '{User}' на топик '{Topic}'",
//                ntfyUsername, topicName);

//            // 1. Создаем пользователя подписки без префикса "v1/"
//            var userPayload = new NtfyUserRequest(ntfyUsername, "user");
//            var userResponse = await _httpClient.PostAsJsonAsync("admin/user", userPayload, cancellationToken);
//            userResponse.EnsureSuccessStatusCode();

//            // 2. Назначаем права доступа без префикса "v1/"
//            var accessPayload = new NtfyAccessRequest(ntfyUsername, topicName, "read");
//            var accessResponse = await _httpClient.PostAsJsonAsync("admin/access", accessPayload, cancellationToken);
//            accessResponse.EnsureSuccessStatusCode();

//            // 3. Генерируем бессрочный токен (expires = 0) через Form URL Encoded
//            // ntfy-server вернет {"token": "tk_..."}, а не пуш-сообщение
//            var formPairs = new List<KeyValuePair<string, string>>
//            {
//                new("username", ntfyUsername),
//                new("expires", "0") // 0 = бессрочный токен
//            };

//            var tokenRequestContent = new FormUrlEncodedContent(formPairs);

//            // Вызываем канонический роут генерации токенов
//            var tokenResponse = await _httpClient.PostAsync("admin/token", tokenRequestContent, cancellationToken);
//            tokenResponse.EnsureSuccessStatusCode();

//            string rawTokenJson = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
//            _logger.LogInformation("[NtfyAuth] Сырой ответ сервера ntfy на запрос токена: {RawJson}", rawTokenJson);

//            var tokenData = System.Text.Json.JsonSerializer.Deserialize<NtfyTokenResponse>(rawTokenJson);
//            if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.Token))
//            {
//                throw new InvalidOperationException($"[NtfyAuth] Сервер ntfy вернул некорректную структуру. Ответ: {rawTokenJson}");
//            }


//            _logger.LogInformation("[NtfyAuth] Токен для фирмы успешно сгенерирован в ntfy.");
//            return tokenData.Token;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "[NtfyAuth] Критический сбой при взаимодействии с HTTP Admin API сервера ntfy.");
//            throw;
//        }
//    }


//    // Вспомогательные неизменяемые DTO-контракты (Clean Code)
//    private record NtfyUserRequest([property: JsonPropertyName("username")] string Username, [property: JsonPropertyName("role")] string Role);
//    private record NtfyAccessRequest([property: JsonPropertyName("username")] string Username, [property: JsonPropertyName("topic")] string Topic, [property: JsonPropertyName("access")] string Access);
//    private record NtfyTokenRequest([property: JsonPropertyName("username")] string Username, [property: JsonPropertyName("expires")] long Expires);
//    private record NtfyTokenResponse([property: JsonPropertyName("token")] string Token);
//}

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SberAzsMonitoring.Dashboard.Application.Interfaces;

namespace SberAzsMonitoring.Dashboard.Infrastructure.Services;

/// <summary>
/// Промышленная реализация службы авторизации ntfy через административный HTTP API.
/// </summary>
public sealed class NtfyAuthService : INtfyAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NtfyAuthService> _logger;

    public NtfyAuthService(HttpClient httpClient, ILogger<NtfyAuthService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Вспомогательный метод настройки канонических заголовков авторизации для HttpClient.
    /// </summary>
    private void ConfigureHttpClient()
    {
        string baseUrlStr = Environment.GetEnvironmentVariable("RegionSettings__NtfyBaseUrl") ?? "http://ntfy-server";
        string adminPassword = Environment.GetEnvironmentVariable("NotificationWorkerOptions__NtfyAdminPassword") ?? "SecureAdminPassword2026!";

        var baseUrl = new Uri(baseUrlStr.EndsWith("/") ? baseUrlStr : baseUrlStr + "/");
        string rawCredentials = $"admin:{adminPassword}";
        string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));

        _httpClient.BaseAddress = baseUrl;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);
    }

    /// <summary>
    /// МЕТОД 1: Вызывается один раз при создании фирмы (в CreateTenantUseCase).
    /// Регистрирует учетную запись в ntfy-server со стабильным паролем Basic Auth.
    /// </summary>
    public async Task<string> RegisterUserAsync(string tenantSystemLogin, CancellationToken cancellationToken)
    {
        ConfigureHttpClient();
        string ntfyPassword = $"p_{tenantSystemLogin}_2026";

        try
        {
            _logger.LogInformation("[NtfyAuth] Первичная регистрация пользователя '{User}' в ntfy-server", tenantSystemLogin);

            var userPayload = new NtfyUserRequest(tenantSystemLogin, ntfyPassword, "user");
            var userResponse = await _httpClient.PostAsJsonAsync("admin/user", userPayload, cancellationToken);
            userResponse.EnsureSuccessStatusCode();

            _logger.LogInformation("[NtfyAuth] Пользователь '{User}' успешно создан", tenantSystemLogin);
            return ntfyPassword; // Этот пароль зашифруется и сохранится в DashboardTenants один раз
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NtfyAuth] Критический сбой при создании пользователя в ntfy-server.");
            throw;
        }
    }

    /// <summary>
    /// МЕТОД 2: Вызывается сколько угодно раз при привязке к 1, 2, 33 топикам (в AddTenantChannelUseCase).
    /// Динамически расширяет права доступа read-only на указанный системный топик.
    /// </summary>
    public async Task GrantAccessAsync(string tenantSystemLogin, string topicName, CancellationToken cancellationToken)
    {
        ConfigureHttpClient();

        try
        {
            _logger.LogInformation("[NtfyAuth] Добавление прав доступа для '{User}' на системный топик '{Topic}'",
                tenantSystemLogin, topicName);

            var accessPayload = new NtfyAccessRequest(tenantSystemLogin, topicName, "read");
            var accessResponse = await _httpClient.PostAsJsonAsync("admin/access", accessPayload, cancellationToken);
            accessResponse.EnsureSuccessStatusCode();

            _logger.LogInformation("[NtfyAuth] Права доступа на топик '{Topic}' успешно зафиксированы в ntfy.", topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NtfyAuth] Критический сбой при назначении прав доступа в ntfy-server на топик '{Topic}'.", topicName);
            throw;
        }
    }

    public async Task RevokeAccessAsync(string username, string topic, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(topic)) return;

        // Формируем запрос согласно спецификации ntfy admin API (стандарт Basic Auth без префикса /v1)
        var requestUrl = "admin/access";

        var payload = new
        {
            username = username,
            topic = topic,
            access = "none" // Полностью отзываем доступ к топику в ACL ntfy
        };

        // Отправляем POST-запрос к ntfy-server
        // (Используем ваш внутренний инжектированный HttpClient или аналогичный метод отправки, развернутый в этом классе)
        var response = await _httpClient.PostAsJsonAsync(requestUrl, payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Не удалось отозвать доступ в ntfy для {username} к топику {topic}. Код: {response.StatusCode}, Ошибка: {errorContent}");
        }
    }


    // Обратная совместимость со старой сигнатурой для предотвращения мгновенного падения компиляции всего проекта
    [Obsolete("Используйте раздельные методы RegisterUserAsync и GrantAccessAsync для поддержки N-подписок.")]
    public async Task<string> CreateSubscriptionTokenAsync(string tenantId, string regionName, string topicName, CancellationToken cancellationToken = default)
    {
        await GrantAccessAsync(tenantId, topicName, cancellationToken);
        return $"p_{tenantId}_2026";
    }
    // Вспомогательные неизменяемые DTO-контракты (Clean Code) с корректным маппингом свойств для Go-сервера
    private record NtfyUserRequest(
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("password")] string Password,
        [property: JsonPropertyName("role")] string Role);

    private record NtfyAccessRequest(
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("topic")] string Topic,
        [property: JsonPropertyName("access")] string Access);
}
