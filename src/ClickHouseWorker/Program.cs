using ClickHouse.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SberAzsMonitoring.ClickHouseWorker.Configurations;
using SberAzsMonitoring.ClickHouseWorker.Data;
using SberAzsMonitoring.ClickHouseWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

var workerOptions = builder.Configuration.GetSection(WorkerOptions.SectionName).Get<WorkerOptions>()
                    ?? new WorkerOptions();
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(WorkerOptions.SectionName));

// Настройка контекста данных внутри воркера
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseClickHouse(workerOptions.ClickHouseConnectionString));

// Запуск фонового консьюмера
builder.Services.AddHostedService<ClickHouseConsumerService>();

var host = builder.Build();

// автосоздание таблицы через прямой нативный SQL без вызова багнутого HasTables()
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    const string createTableSql = @"
        CREATE TABLE IF NOT EXISTS default.fuel_station_snapshots
        (
            region LowCardinality(String),
            station_id String,
            station_name String,
            station_address String,
            latitude Float64,
            longitude Float64,
            fuel_type LowCardinality(String),
            is_available UInt8,
            availability_status String,
            limit_liters Int32,
            timestamp DateTime64(3, 'UTC'),
            event_id UUID
        )
        ENGINE = ReplacingMergeTree(timestamp)
        PRIMARY KEY (region, station_id, fuel_type)
        ORDER BY (region, station_id, fuel_type, timestamp);";

    try
    {
        // Выполняем сырой SQL запрос напрямую в СУБД. Это сработает на любой версии ClickHouse
        db.Database.ExecuteSqlRaw(createTableSql);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
        logger.LogCritical(ex, "Критическая ошибка инициализации структуры таблиц в ClickHouse.");
        throw;
    }
}

await host.RunAsync();

