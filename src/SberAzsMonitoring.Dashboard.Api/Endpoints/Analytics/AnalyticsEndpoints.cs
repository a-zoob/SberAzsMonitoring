using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces.Analytics;
using SberAzsMonitoring.Dashboard.Application.UseCases.Analytics;

namespace SberAzsMonitoring.Dashboard.Api.Endpoints.Analytics;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics");

        // Эндпоинт для получения текущего статуса АЗС
        group.MapPost("/current-status", async (
            [FromBody] IEnumerable<string>? regions,
            [FromServices] GetLatestFuelAvailabilityUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(regions, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetLatestFuelAvailability")
        .WithTags("Analytics")
        .WithSummary("Актуальная доступность бензина")
        .WithDescription("Получение актуальных данных о доступности бензина на АЗС");

        // Эндпоинт для получения списка доступных регионов
        group.MapGet("/regions", async (
            [FromServices] IClickHouseAnalyticsRepository repository,
            CancellationToken cancellationToken) =>
        {
            var result = await repository.GetAvailableRegionsAsync(cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetAvailableRegions")
        .WithTags("Analytics")
        .WithSummary("Список регионов для фильтрации")
        .WithDescription("Получение списка регионов");

        // Эндпоинт для получения списка доступных марок бензина
        group.MapGet("/fuel-types", async (
            [FromServices] IClickHouseAnalyticsRepository repository,
            CancellationToken cancellationToken) =>
        {
            var result = await repository.GetAvailableFuelTypesAsync(cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetAvailableFuelTypes")
        .WithTags("Analytics")
        .WithSummary("Список марок бензина для фильтрации")
        .WithDescription("Получение списка марок бензина");
    }
}
