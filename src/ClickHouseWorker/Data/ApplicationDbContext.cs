// FILE: \src\ClickHouseWorker\Data\ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using SberAzsMonitoring.Domain.Entities;

namespace SberAzsMonitoring.ClickHouseWorker.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<FuelStationSnapshot> FuelStationSnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FuelStationSnapshot>(entity =>
        {
            // Указываем точное имя таблицы в ClickHouse
            entity.ToTable("fuel_station_snapshots");

            // Задаем составной первичный ключ
            entity.HasKey(e => new { e.Region, e.StationId, e.FuelType, e.Timestamp });

            // ЖЕСТКИЙ МАППИНГ: Связываем PascalCase свойства C# с snake_case колонками в ClickHouse
            entity.Property(e => e.Region).HasColumnName("region").HasColumnType("LowCardinality(String)");
            entity.Property(e => e.StationId).HasColumnName("station_id");
            entity.Property(e => e.StationName).HasColumnName("station_name");
            entity.Property(e => e.StationAddress).HasColumnName("station_address");
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasColumnName("longitude");

            entity.Property(e => e.FuelType).HasColumnName("fuel_type").HasColumnType("LowCardinality(String)");
            entity.Property(e => e.IsAvailable).HasColumnName("is_available");
            entity.Property(e => e.AvailabilityStatus).HasColumnName("availability_status");
            entity.Property(e => e.LimitLiters).HasColumnName("limit_liters");

            entity.Property(e => e.Timestamp).HasColumnName("timestamp").HasColumnType("DateTime64(3, 'UTC')");
            entity.Property(e => e.EventId).HasColumnName("event_id");

            // Конфигурируем движок ReplacingMergeTree по имени колонки времени в нижнем регистре (timestamp)
            entity.HasAnnotation("ClickHouse:Engine", "ReplacingMergeTree(timestamp)");
        });
    }
}
