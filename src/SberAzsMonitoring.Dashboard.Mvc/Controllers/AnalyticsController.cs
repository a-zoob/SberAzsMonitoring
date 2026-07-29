using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Json;
using SberAzsMonitoring.Dashboard.Mvc.Models;

namespace SberAzsMonitoring.Dashboard.Mvc.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly HttpClient _apiClient;
        private readonly ILogger<AnalyticsController> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Внедряем логер вместе с фабрикой HTTP-клиентов
        public AnalyticsController(IHttpClientFactory httpClientFactory, ILogger<AnalyticsController> logger)
        {
            _apiClient = httpClientFactory.CreateClient("BackendApi");
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var regions = new List<string>();
            var fuelTypes = new List<string>();

            try
            {
                _logger.LogInformation("Запрос справочников регионов и марок топлива из бэкенд-API.");

                var regionsTask = _apiClient.GetAsync("api/analytics/regions");
                var fuelTypesTask = _apiClient.GetAsync("api/analytics/fuel-types");

                await Task.WhenAll(regionsTask, fuelTypesTask);

                if (regionsTask.Result.IsSuccessStatusCode)
                {
                    using var contentStream = await regionsTask.Result.Content.ReadAsStreamAsync();
                    regions = await JsonSerializer.DeserializeAsync<List<string>>(contentStream, _jsonOptions) ?? new();
                }
                else
                {
                    _logger.LogWarning("Не удалось загрузить регионы. Статус: {StatusCode}", regionsTask.Result.StatusCode);
                }

                if (fuelTypesTask.Result.IsSuccessStatusCode)
                {
                    using var contentStream = await fuelTypesTask.Result.Content.ReadAsStreamAsync();
                    fuelTypes = await JsonSerializer.DeserializeAsync<List<string>>(contentStream, _jsonOptions) ?? new();
                }
                else
                {
                    _logger.LogWarning("Не удалось загрузить марки топлива. Статус: {StatusCode}", fuelTypesTask.Result.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при первичной инициализации фильтров панели мониторинга.");
                ViewBag.ErrorMessage = $"Ошибка связи с аналитическим бэкендом: {ex.Message}";
            }

            ViewBag.Regions = regions;
            ViewBag.FuelTypes = fuelTypes;

            return View();
        }

        //[HttpGet]
        //public async Task<IActionResult> GetLatestAvailability(string region, string fuelType)
        //{
        //    if (string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(fuelType))
        //    {
        //        return BadRequest(new { message = "Регион и марка топлива обязательны." });
        //    }

        //    try
        //    {
        //        _logger.LogInformation("Запрос актуального среза данных для региона: {Region}, марка: {FuelType}", region, fuelType);

        //        var regionsPayload = new List<string> { region };
        //        var response = await _apiClient.PostAsJsonAsync("api/analytics/current-status", regionsPayload);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            _logger.LogWarning("Бэкенд вернул ошибку при запросе статуса. Код: {StatusCode}", response.StatusCode);
        //            return StatusCode((int)response.StatusCode, new { message = "Бэкенд вернул ошибку при получении данных." });
        //        }

        //        using var contentStream = await response.Content.ReadAsStreamAsync();
        //        var rawData = await JsonSerializer.DeserializeAsync<List<StationAvailabilityDto>>(contentStream, _jsonOptions) ?? new();

        //        var filteredData = rawData
        //            .Where(x => string.Equals(x.FuelType, fuelType, StringComparison.OrdinalIgnoreCase))
        //            .ToList();

        //        _logger.LogInformation("Успешно обработано {Count} записей для региона {Region}", filteredData.Count, region);

        //        return Json(filteredData);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Структурное логирование с передачей параметров (шаблонизация)
        //        _logger.LogError(ex, "Ошибка при выполнении прокси-запроса среза данных. Регион: {Region}", region);
        //        return StatusCode(500, new { message = $"Внутренняя ошибка проксирования: {ex.Message}" });
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> GetLatestAvailability(string region, string fuelType)
        {
            if (string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(fuelType))
            {
                return BadRequest(new { message = "Регион и марка топлива обязательны." });
            }

            try
            {
                _logger.LogInformation("Запрос актуального среза данных для региона: {Region}, марка: {FuelType}", region, fuelType);

                var regionsPayload = new List<string> { region };
                var response = await _apiClient.PostAsJsonAsync("api/analytics/current-status", regionsPayload);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Бэкенд вернул ошибку при запросе статуса. Код: {StatusCode}", response.StatusCode);
                    return StatusCode((int)response.StatusCode, new { message = "Бэкенд вернул ошибку при получении данных." });
                }

                using var contentStream = await response.Content.ReadAsStreamAsync();
                var rawData = await JsonSerializer.DeserializeAsync<List<StationAvailabilityDto>>(contentStream, _jsonOptions) ?? new();

                var latestSnapshotPerStation = rawData
                .GroupBy(x => new { x.StationId, x.FuelType })
                .Select(group => group.OrderByDescending(x => x.Timestamp).First())
                .ToList();

                // Фильтруем полученный актуальный срез по выбранной марке топлива
                var filteredData = latestSnapshotPerStation
                    .Where(x => string.Equals(x.FuelType, fuelType, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.StationName)
                    .ToList();

                _logger.LogInformation("Успешно отфильтрован актуальный срез: {Count} уникальных АЗС для региона {Region}", filteredData.Count, region);

                return Json(filteredData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении прокси-запроса среза данных. Регион: {Region}", region);
                return StatusCode(500, new { message = $"Внутренняя ошибка проксирования: {ex.Message}" });
            }
        }

    }
}
