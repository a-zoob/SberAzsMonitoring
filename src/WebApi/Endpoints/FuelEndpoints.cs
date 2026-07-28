using SberAzsMonitoring.Application.Features.Commands;
using Microsoft.AspNetCore.Mvc;
using SberAzsMonitoring.Application.Interfaces;

namespace SberAzsMonitoring.WebApi.Endpoints;

public static class FuelEndpoints
{
    public static void MapFuelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fuel");

        group.MapGet("/current", async ([FromServices] IFuelRepository fuelRepository) =>
        {
            var currentStations = await fuelRepository.GetCurrentStationsAsync();
            return Results.Ok(currentStations);
        });

        group.MapGet("/history", async (string stationId, string fuelType, [FromServices] IFuelRepository fuelRepository) =>
        {
            var historyData = await fuelRepository.GetPriceHistoryAsync(stationId, fuelType);
            return Results.Ok(new { StationId = stationId, FuelType = fuelType, History = historyData });
        });

        // 3. Сокращенный управляющий эндпоинт, вызывающий хендлер
        group.MapPost("/notify", async (
            HttpContext context,
            [FromServices] INotifyRegionScanHandler handler) =>
        {
            // Извлекаем заголовок X-Tenant-Id, если запрос инициирован конкретной фирмой из Дашборда
            string? targetTenantId = null;
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValues))
            {
                targetTenantId = tenantIdValues.ToString();
            }

            // Инициализируем команду, передавая в неё опциональный маркер фирмы-получателя
            var command = new NotifyRegionScanCommand(targetTenantId);

            // Используем встроенный в ASP.NET CancellationToken для автоматической отмены при разрыве соединения
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, context.RequestAborted);

            var result = await handler.HandleAsync(command, linkedCts.Token);

            return result.IsSuccess
                ? Results.Ok(new { Status = "Success", Message = result.Message })
                : Results.BadRequest(new { Status = "Error", Message = result.Message });
        });
    }
}
