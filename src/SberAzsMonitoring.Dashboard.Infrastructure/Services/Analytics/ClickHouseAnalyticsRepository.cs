using System.Data;
using Microsoft.Extensions.Configuration;
using ClickHouse.Client.ADO; 
using SberAzsMonitoring.Dashboard.Application.Common.DTOs.Analytics;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces.Analytics;

namespace SberAzsMonitoring.Dashboard.Infrastructure.Services.Analytics;

public class ClickHouseAnalyticsRepository : IClickHouseAnalyticsRepository
{
    private readonly string _connectionString;

    public ClickHouseAnalyticsRepository(IConfiguration configuration)
    {
        // Берем строку подключения из конфигурации Дашборда
        _connectionString = configuration.GetConnectionString("ClickHouseConnection")
            ?? throw new ArgumentNullException("Connection string 'ClickHouseConnection' not found.");
    }

    public async Task<IEnumerable<StationAvailabilityDto>> GetCurrentRegistryAsync(
        IEnumerable<string>? regions = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = new ClickHouseConnection(_connectionString);
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        using var command = connection.CreateCommand();

        string sql = @"
            SELECT 
                region,
                station_id,
                station_name,
                station_address,
                fuel_type,
                is_available,
                availability_status,
                limit_liters,
                timestamp
            FROM fuel_station_snapshots FINAL";

        var regionList = regions?.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        if (regionList != null && regionList.Any())
        {
            var formattedRegions = string.Join(",", regionList.Select(r => $"'{r.Replace("'", "''")}'"));
            sql += $" WHERE region IN ({formattedRegions})";
        }

        command.CommandText = sql;

        var result = new List<StationAvailabilityDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new StationAvailabilityDto(
                 Region: reader.GetString(0),
                 StationId: reader.GetString(1),
                 StationName: reader.GetString(2),
                 StationAddress: reader.GetString(3),
                 FuelType: reader.GetString(4),
                 IsAvailable: reader.GetByte(5) == 1,
                 AvailabilityStatus: reader.GetString(6),
                 LimitLiters: reader.GetInt32(7),
                 Timestamp: reader.GetDateTime(8)
             ));
        }

        return result;
    }

    // Получить список всех уникальных регионов из ClickHouse
    public async Task<IEnumerable<string>> GetAvailableRegionsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new ClickHouse.Client.ADO.ClickHouseConnection(_connectionString);
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT region FROM fuel_station_snapshots ORDER BY region ASC";

        var regions = new List<string>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            regions.Add(reader.GetString(0));
        }

        return regions;
    }

    // Получить список всех уникальных марок бензина из ClickHouse
    public async Task<IEnumerable<string>> GetAvailableFuelTypesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new ClickHouse.Client.ADO.ClickHouseConnection(_connectionString);
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT fuel_type FROM fuel_station_snapshots ORDER BY fuel_type ASC";

        var fuelTypes = new List<string>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            fuelTypes.Add(reader.GetString(0));
        }

        return fuelTypes;
    }

}
