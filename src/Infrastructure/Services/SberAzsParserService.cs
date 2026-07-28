using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SberAzsMonitoring.Application.Interfaces;
using SberAzsMonitoring.Application.Common.Configurations;
using SberAzsMonitoring.Domain;
using SberAzsMonitoring.Infrastructure.Models;

namespace SberAzsMonitoring.Infrastructure.Services;

public class SberAzsParserService : IFuelParserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SberAzsParserService> _logger;
    private readonly string _apiUrl;

    public SberAzsParserService(HttpClient httpClient, ILogger<SberAzsParserService> logger, IOptions<RegionOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Адрес берется строго из настроек конкретного инстанса контейнера
        _apiUrl = options.Value.SberAzsEndpoint;

        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<IEnumerable<FuelStation>> ParseActualPricesAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiUrl))
        {
            _logger.LogError("Критическая ошибка: SberAzsEndpoint не задан в конфигурации региона!");
            return Enumerable.Empty<FuelStation>();
        }

        _logger.LogInformation("Опрос реального сервера Сбера: {Url}", _apiUrl);
        try
        {
            using var response = await _httpClient.GetAsync(_apiUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var rawJsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(rawJsonString))
            {
                _logger.LogWarning("Сервер Сбера вернул пустой текстовый ответ.");
                return Enumerable.Empty<FuelStation>();
            }

            var apiData = JsonSerializer.Deserialize<SberAzsApiResponse>(rawJsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (apiData?.Stations == null || !apiData.Stations.Any())
            {
                _logger.LogWarning("Сервер Сбера вернул пустой массив станций или JSON не распознан.");
                return Enumerable.Empty<FuelStation>();
            }

            _logger.LogInformation("Успешно получено {Count} АЗС от Сбера. Начинаем маппинг...", apiData.Stations.Count);

            return apiData.Stations.Select(s => new FuelStation
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                Latitude = s.Location.Lat,
                Longitude = s.Location.Lon,
                LastUpdatedAt = DateTime.UtcNow,
                FuelStates = s.Fuels.Select(f => new FuelState
                {
                    FuelType = f.Type,
                    // отрицание '!' перед проверкой на пустоту
                    //IsAvailable = !string.IsNullOrEmpty(f.AvailabilityStatus) && f.AvailabilityStatus.Equals("available", StringComparison.OrdinalIgnoreCase),
                    IsAvailable = f.AvailabilityStatus?.Equals("available", StringComparison.OrdinalIgnoreCase) ?? false,
                    AvailabilityStatus = f.AvailabilityStatus ?? "unknown",
                    LimitLiters = f.LimitLiters ?? 0
                }).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при парсинге или запросе к Сберу.");
            return Enumerable.Empty<FuelStation>();
        }
    }
}
