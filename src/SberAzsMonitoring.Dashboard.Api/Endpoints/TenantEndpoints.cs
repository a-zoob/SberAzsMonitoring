using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SberAzsMonitoring.Dashboard.Application.UseCases;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.Dashboard.Api.Endpoints;

/// <summary>
/// Класс-картограф для эндпоинтов управления фирмами (тенантами) Дашборда.
/// </summary>
public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard/tenants");

        // 1. Эндпоинт создания (регистрации) новой фирмы со строгим балансом 0
        group.MapPost("/", async (
            [FromBody] CreateTenantDto request,
            [FromServices] CreateTenantUseCase createTenantUseCase,
            CancellationToken cancellationToken) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Название фирмы является обязательным полем." });
            }
            try
            {
                Guid tenantId = await createTenantUseCase.ExecuteAsync(
                    request.Name,
                    request.RawNtfyToken,
                    cancellationToken);
                return Results.Created($"/api/dashboard/tenants/{tenantId}", new { id = tenantId, message = "Фирма успешно зарегистрирована со стартовым балансом 0.00." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
            .WithName("CreateTenant")
   .WithTags("Tenants Management")
   .WithSummary("Регистрация новой фирмы")
   .WithDescription("Создает Guid фирмы, пользователя в ntfy со стабильным паролем, шифрует его в AES-256 и коммитит в PostgreSQL.");

        // 2. Эндпоинт обновления/корректировки баланса фирмы
        group.MapPut("/{id:guid}/balance", async (
            [FromRoute] Guid id,
            [FromBody] UpdateBalanceDto request,
            [FromServices] UpdateTenantBalanceUseCase updateBalanceUseCase,
            CancellationToken cancellationToken) =>
        {
            if (request == null || request.NewBalance < 0)
            {
                return Results.BadRequest(new { error = "Указано невалидное значение баланса. Баланс не может быть отрицательным." });
            }
            try
            {
                await updateBalanceUseCase.ExecuteAsync(id, request.NewBalance, cancellationToken);
                return Results.Ok(new { success = true, message = "Баланс фирмы успешно обновлен." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
            .WithName("UpdateTenantBalance")
   .WithTags("Tenants Management")
   .WithSummary("Пополнение баланса фирмы")
   .WithDescription("Изменяет финансовый баланс фирмы в БД с фиксацией транзакции и защитой от рассинхронизации JSON-полей.");



        // 3. Эндпоинт привязки фирмы к системному топику мониторинга АЗС из docker-compose
        group.MapPost("/{id:guid}/channels", async (
            [FromRoute] Guid id,
            [FromBody] AddTenantChannelDto request,
            [FromServices] AddTenantChannelUseCase addChannelUseCase,
            CancellationToken cancellationToken) =>
        {
            try
            {
                // Передаем строго отвалидированное системное имя топика напрямую в Use Case
                await addChannelUseCase.ExecuteAsync(id, request.SysTopicName, cancellationToken);

                return Results.Ok(new
                {
                    success = true,
                    message = $"Фирма успешно привязана к системному топику '{request.SysTopicName}'."
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (Exception)
            {
                return Results.InternalServerError(new
                {
                    error = "Внутренняя ошибка сервера при инициализации канала подписки."
                });
            }
        })
             .WithName("AddTenantChannel")
   .WithTags("Tenant Channels")
   .WithSummary("Привязать фирму к региону/топику АЗС")
   .WithDescription("Расширяет ACL-карту прав доступа в ntfy-server через GrantAccessAsync без перезаписи токена фирмы.");


        group.MapGet("/{id:guid}/credentials", async (
    [FromRoute] Guid id,
    [FromServices] GetTenantCredentialsUseCase getCredentialsUseCase,
    CancellationToken cancellationToken) =>
        {
            try
            {
                var credentials = await getCredentialsUseCase.ExecuteAsync(id, cancellationToken);
                return Results.Ok(credentials);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception)
            {
                return Results.InternalServerError(new { error = "Внутренняя ошибка сервера при дешифрации токена доступа." });
                //return Results.StatusCode(StatusCodes.Status500InternalServerError,
                //    new { error = "Внутренняя ошибка сервера при дешифрации токена доступа." });
            }
        })
            .WithName("GetTenantCredentials")
   .WithTags("Tenants Credentials")
   .WithSummary("Получить реквизиты авторизации для смартфона")
   .WithDescription("Дешифрует сохраненный хэш из БД и отдает чистые login и accessToken (пароль) для ввода в мобильное приложение ntfy.");

        // Получаем профиль фирмы.
        group.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] GetTenantProfileUseCase getProfileUseCase,
            CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var profile = await getProfileUseCase.ExecuteAsync(id, cancellationToken);
                        return Results.Ok(profile);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.NotFound(new { error = ex.Message });
                    }
                    catch (Exception)
                    {
                        return Results.InternalServerError(new { error = "Внутренняя ошибка сервера при получении профиля фирмы." });
                    }
                })
            .WithName("GetTenantProfile")
   .WithTags("Tenants Management")
   .WithSummary("Получить полный профиль фирмы")
   .WithDescription("Возвращает Guid, название, баланс и весь массив активных каналов подписок из PostgreSQL.");

        app.MapDelete("/api/dashboard/tenants/{id:guid}/channels/{sysTopicName}", async (
        Guid id,
        string sysTopicName,
        RemoveTenantChannelUseCase useCase,
        CancellationToken cancellationToken) =>
        {
            try
            {
                // Вызываем наш атомарный Use Case
                await useCase.ExecuteAsync(id, sysTopicName, cancellationToken);

                // Возвращаем статус 200 OK при успешном удалении
                return Results.Ok(new { message = $"Фирма успешно отвязана от топика {sysTopicName}. Доступ в ntfy аннулирован." });
            }
            catch (KeyNotFoundException ex)
            {
                // Если фирма не найдена в БД — 404 Not Found
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Если фирма не была подписана на этот топик (ошибка валидации модели) — 400 Bad Request
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                // Системные или сетевые сбои — 500 Internal Server Error
                return Results.Json(new { error = "Внутренняя ошибка при удалении канала.", details = ex.Message }, statusCode: 500);
            }
        })
            .WithName("RemoveTenantChannel")
            .WithTags("Tenant Channels") // Группировка в UI
            .WithSummary("Отвязать фирму от топика АЗС")
            .WithDescription("Удаляет подписку из PostgreSQL, отзывает ACL права в ntfy-server и публикует обновление в Kafka.");

                app.MapGet("/api/dashboard/tenants", async (
                GetAllTenantsUseCase useCase,
                CancellationToken cancellationToken) =>
                {
                    try
                    {
                        // Вызываем наш оптимизированный Use Case чтения
                        var tenants = await useCase.ExecuteAsync(cancellationToken);

                        // Возвращаем список фирм со статусом 200 OK
                        return Results.Ok(tenants);
                    }
                    catch (Exception ex)
                    {
                        // Обработка непредвиденных системных сбоев
                        return Results.Json(new { error = "Внутренняя ошибка при получении списка фирм.", details = ex.Message }, statusCode: 500);
                    }
                })
            .WithName("GetAllTenants")
            .WithTags("Tenants Management") // Автоматически группируем в Scalar UI в ту же папку
            .WithSummary("Получить список всех фирм")
            .WithDescription("Возвращает список всех зарегистрированных компаний с указанием их ID, баланса и количества активных каналов подписок.");

        return app;
    }
}

/// <summary>
/// Безопасный DTO-контракт для создания фирмы без лазейки с балансом.
/// </summary>
public sealed record CreateTenantDto(string Name, string RawNtfyToken);

/// <summary>
/// DTO-контракт для обновления финансового баланса фирмы.
/// </summary>
public sealed record UpdateBalanceDto
{
    public required decimal NewBalance { get; init; }
}

/// <summary>
/// DTO-контракт для привязки фирмы к региону с автоматической генерацией топика ntfy.
/// </summary>
public sealed record AddChannelDto(string RegionName);

// Объявление DTO контракта для привязки канала по системному имени топика
public sealed record AddTenantChannelDto(string SysTopicName);

