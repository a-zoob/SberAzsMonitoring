using System.Net;
using System.Net.Http.Json;
using Moq;
using SberAzsMonitoring.Dashboard.Application.Common.DTOs.Analytics;

namespace SberAzsMonitoring.Dashboard.IntegrationTests;

public class AnalyticsEndpointsTests : IClassFixture<DashboardWebApplicationFactory>
{
    private readonly DashboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AnalyticsEndpointsTests(DashboardWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrentStatus_WithValidRegions_ReturnsOkAndCorrectData()
    {
        // Arrange (Подготовка данных)
        var requestedRegions = new[] { "pskov" };
        var expectedTimestamp = DateTime.UtcNow;

        var mockData = new List<StationAvailabilityDto>
        {
            new(
                Region: "pskov",
                StationId: "101",
                StationName: "АЗС Сбер N101",
                StationAddress: "г. Псков, ул. Ленина, 15",
                FuelType: "ai95",
                IsAvailable: true,
                AvailabilityStatus: "Доступно",
                LimitLiters: 0,
                Timestamp: expectedTimestamp
            )
        };

        // Настраиваем поведение Mock-репозитория
        _factory.ClickHouseRepositoryMock
            .Setup(repo => repo.GetCurrentRegistryAsync(
                It.Is<IEnumerable<string>>(r => r != null && r.Contains("pskov")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        // Act (Выполнение действия)
        var response = await _client.PostAsJsonAsync("/api/analytics/current-status", requestedRegions);

        // Assert (Проверка утверждений)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<IEnumerable<StationAvailabilityDto>>();
        Assert.NotNull(result);

        var station = Assert.Single(result);
        Assert.Equal("pskov", station.Region);
        Assert.Equal("101", station.StationId);
        Assert.Equal("ai95", station.FuelType);
        Assert.True(station.IsAvailable);
    }

    [Fact]
    public async Task GetAvailableRegions_ReturnsOkAndListofRegions()
    {
        // Arrange
        var mockRegions = new List<string> { "novgorod", "pskov" };

        _factory.ClickHouseRepositoryMock
            .Setup(repo => repo.GetAvailableRegionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockRegions);

        // Act
        var response = await _client.GetAsync("/api/analytics/regions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<IEnumerable<string>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains("pskov", result);
        Assert.Contains("novgorod", result);
    }

    [Fact]
    public async Task GetAvailableFuelTypes_ReturnsOkAndListofFuelTypes()
    {
        // Arrange
        var mockFuelTypes = new List<string> { "ai92", "ai95" };

        _factory.ClickHouseRepositoryMock
            .Setup(repo => repo.GetAvailableFuelTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFuelTypes);

        // Act
        var response = await _client.GetAsync("/api/analytics/fuel-types");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<IEnumerable<string>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains("ai95", result);
        Assert.Contains("ai92", result);
    }

}
