using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SberAzsMonitoring.Dashboard.Application.UseCases;
using System.Threading;

namespace SberAzsMonitoring.Dashboard.Api.Endpoints;

/// <summary>
/// Класс-картограф для изоляции эндпоинтов аутентификации Дашборда.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard/auth");

        group.MapPost("/login", async (
            [FromBody] LoginDto request,
            [FromServices] AuthenticateDashboardUserUseCase authenticateUseCase,
            CancellationToken cancellationToken) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Логин и пароль обязательны к заполнению." });
            }

            bool isAuthenticated = await authenticateUseCase.ExecuteAsync(request.Login, request.Password, cancellationToken);

            if (!isAuthenticated)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new { success = true, message = "Аутентификация пройдена успешно." });
        })
            .WithName("LoginDashboardUser")
       .WithTags("Dashboard Security")
       .WithSummary("Вход в панель управления")
       .WithDescription("Верифицирует данные пользователя и выдает сессионный токен доступа к функциям Дашборда.");

        return app;
    }
}

/// <summary>
/// DTO-контракт для входящего запроса авторизации.
/// </summary>
public sealed record LoginDto(string Login, string Password);
