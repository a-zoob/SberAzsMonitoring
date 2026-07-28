using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SberAzsMonitoring.Application.Common.DTOs;
using SberAzsMonitoring.Application.Common.Contracts; // Для RegionScanIntegrationEvent
using SberAzsMonitoring.Application.Interfaces;
using SberAzsMonitoring.Application.Common.Configurations;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.Application.Features.Commands;

public class NotifyRegionScanCommandHandler : INotifyRegionScanHandler
{
    private readonly IFuelParserService _parserService;
    private readonly IKafkaProducerService _kafkaProducerService;
    private readonly RegionOptions _regionOptions;
    private readonly ILogger<NotifyRegionScanCommandHandler> _logger;
    private static string _lastAlertMessage = string.Empty;

    public NotifyRegionScanCommandHandler(
        IFuelParserService parserService,
        IKafkaProducerService kafkaProducerService,
        IOptions<RegionOptions> options,
        ILogger<NotifyRegionScanCommandHandler> logger)
    {
        _parserService = parserService;
        _kafkaProducerService = kafkaProducerService;
        _regionOptions = options.Value;
        _logger = logger;
    }

    public async Task<NotifyResultDto> HandleAsync(NotifyRegionScanCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запуск сканирования для региона: {Region}", _regionOptions.Name);

        var scrapedStations = await _parserService.ParseActualPricesAsync(cancellationToken);
        var stationList = scrapedStations.ToList();

        if (!stationList.Any())
            return new NotifyResultDto(false, "Парсер вернул пустой массив данных.");

        var currentTime = DateTime.UtcNow;

        // 1. Формируем интеграционное событие для отправки в Apache Kafka
        var scanEvent = new RegionScanIntegrationEvent
        {
            EventId = Guid.NewGuid(),
            OccurredOn = currentTime,
            RegionName = _regionOptions.Name,
            TargetTenantId = command.TargetTenantId,
            Stations = stationList.Select(s => new FuelStationContract
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Fuels = s.FuelStates.Select(f => new FuelStateContract
                {
                    FuelType = f.FuelType,
                    IsAvailable = f.IsAvailable,
                    AvailabilityStatus = f.AvailabilityStatus,
                    LimitLiters = f.LimitLiters
                }).ToList()
            }).ToList()
        };

        // 2. Публикуем событие в Kafka
        await _kafkaProducerService.PublishRegionScanAsync(scanEvent, cancellationToken);

        // 3. Формируем DTO для локального анализа пуш-уведомлений
        var stationDtos = stationList.Select(s => new FuelStationDto(
            s.Id, s.Name, s.Address, s.Latitude, s.Longitude,
            s.FuelStates.Select(f => new FuelStateDto(f.FuelType, f.IsAvailable, f.AvailabilityStatus, f.LimitLiters)).ToList(),
            currentTime
        )).ToList();

        var activeStations = stationDtos
            .Where(s => s.Fuels.Any(f => f.FuelType.StartsWith("ai", StringComparison.OrdinalIgnoreCase) && f.IsAvailable))
            .ToList();

        string currentAlertMessage;

        // Изолированная логика обработки тотального дефицита бензина
        if (!activeStations.Any())
        {
            _logger.LogWarning("В регионе {_regionOptions.Name} зафиксирован тотальный дефицит бензина.", _regionOptions.Name);
            currentAlertMessage = "Ни на одной АЗС не зафиксировано наличие бензина";
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Сводка {_regionOptions.Name}. Бензин в наличии:");

            foreach (var s in activeStations)
            {
                var availableFuels = s.Fuels
                    .Where(f => f.FuelType.StartsWith("ai", StringComparison.OrdinalIgnoreCase) && f.IsAvailable)
                    .Select(f => $"{f.FuelType.ToUpper()} ({f.LimitLiters} л)");

                sb.AppendLine($". {s.Name}, {s.Address} - {string.Join(", ", availableFuels)}");
            }

            sb.Append("На остальных АЗС города дефицит!");
            currentAlertMessage = sb.ToString();
        }

        //// Проверка на дублирование сообщений (Idempotency Filter)
        //if (currentAlertMessage == _lastAlertMessage)
        //{
        //    return new NotifyResultDto(true, "Данные отправлены в Kafka. Изменений для пуша нет.");
        //}

        //_lastAlertMessage = currentAlertMessage;


        return new NotifyResultDto(true, "Данные отправлены в Kafka!");
    }
}
