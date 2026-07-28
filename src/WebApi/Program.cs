using SberAzsMonitoring.Application.Common.Configurations;
using SberAzsMonitoring.Application.Features.Commands;
using SberAzsMonitoring.Application.Interfaces;
using SberAzsMonitoring.Infrastructure.Repositories;
using SberAzsMonitoring.Infrastructure.Services;
using SberAzsMonitoring.WebApi.Endpoints;
using SberAzsMonitoring.WebApi.Services;


var builder = WebApplication.CreateBuilder(args);

// Загружаем настройки региона (нужны только Sber API, Ntfy и Продюсер Kafka)
builder.Services.Configure<RegionOptions>(builder.Configuration.GetSection(RegionOptions.SectionName));

// локальный репозиторий-заглушка или оперативная память (для логики дубликатов пушей)
builder.Services.AddScoped<IFuelRepository, FuelRepository>();

// Регистрируем HttpClient-сервисы с таймаутами
builder.Services.AddHttpClient<IFuelParserService, SberAzsParserService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

//builder.Services.AddHttpClient<INotificationService, NtfyNotificationService>();
builder.Services.AddHttpClient<INotificationService, NtfyNotificationService>((sp, client) =>
{
    // ИСПРАВЛЕНО: Указано корректное пространство имен для интерфейса IOptions
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RegionOptions>>().Value;

    if (string.IsNullOrWhiteSpace(options.NtfyBaseUrl))
    {
        throw new InvalidOperationException("Критическая ошибка: Параметр 'RegionOptions.NtfyBaseUrl' не задан в конфигурации региона.");
    }

    // Передаем адрес, строго настроенный через инфраструктуру docker-compose
    client.BaseAddress = new Uri(options.NtfyBaseUrl);
});

// Регистрируем хендлер CQRS архитектуры и продюсер Kafka
builder.Services.AddScoped<INotifyRegionScanHandler, NotifyRegionScanCommandHandler>();
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

// Оставляем только планировщик опроса Сбера
builder.Services.AddHostedService<FuelBackgroundScheduler>();


var app = builder.Build();





app.MapFuelEndpoints();
app.Run();
