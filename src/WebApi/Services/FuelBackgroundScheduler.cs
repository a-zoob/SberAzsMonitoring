using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SberAzsMonitoring.Application.Features.Commands;
using SberAzsMonitoring.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.WebApi.Services;

public class FuelBackgroundScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FuelBackgroundScheduler> _logger;
    private readonly PeriodicTimer _timer;

    public FuelBackgroundScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<FuelBackgroundScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        // Настраиваем точный таймер на 15 минут (без дрейфа времени)
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Фоновый планировщик мониторинга АЗС успешно запущен. Интервал: 15 минут.");

        try
        {
            // АВТОПИНОК: Даем Кафке 60 секунд на инициализацию портов в Docker.
            // Это гарантирует, что самый первый опрос Сбера не упадет из-за неготовности сети брокера.
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await ExecuteScanJobAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            // Защита: если Кафка все еще лежит, пишем лог, но НЕ ломаем поток службы!
            _logger.LogError(ex, "Стартовый регламентный срез завершился с ошибкой. Ожидаем следующий тик по таймеру.");
        }

        // Цикл периодического срабатывания по таймеру (работает железно в любом случае)
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await ExecuteScanJobAsync(stoppingToken);
        }
    }

    private async Task ExecuteScanJobAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Планировщик инициирует регламентный срез данных АЗС...");

        // Создаем область видимости (Scope) для безопасного извлечения Scoped-сервисов (хендлеров и DB)
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<INotifyRegionScanHandler>();

        try
        {
            // Задаем жесткий таймаут на выполнение всей цепочки (опрос Сбера + пуш + Кафка) в 30 секунд
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var command = new NotifyRegionScanCommand();
            var result = await handler.HandleAsync(command, cts.Token);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Регламентный срез успешно обработан: {Message}", result.Message);
            }
            else
            {
                _logger.LogWarning("Планировщик зафиксировал нештатную ситуацию: {Message}", result.Message);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Работа планировщика остановлена по сигналу завершения приложения.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка при выполнении регламентного сканирования в планировщике.");
        }
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}
