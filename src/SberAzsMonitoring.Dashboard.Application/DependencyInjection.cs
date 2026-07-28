using Microsoft.Extensions.DependencyInjection;
using SberAzsMonitoring.Dashboard.Application.UseCases;

namespace SberAzsMonitoring.Dashboard.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует сервисы бизнес-логики (Use Cases) подсистемы Дашборда в DI-контейнере.
    /// </summary>
    public static IServiceCollection AddDashboardApplication(this IServiceCollection services)
    {
        // Регистрация бизнес-сценария создания нового администратора
        services.AddScoped<CreateDashboardUserUseCase>();

        // Регистрация бизнес-сценария аутентификации пользователя при входе
        services.AddScoped<AuthenticateDashboardUserUseCase>();

        // Регистрация бизнес-сценария создания (регистрации) новой фирмы
        services.AddScoped<CreateTenantUseCase>();

        // Регистрация бизнес-сценария корректировки / пополнения баланса фирмы
        services.AddScoped<UpdateTenantBalanceUseCase>();

        // Регистрация бизнес-сценария привязки фирмы к региональному топику мониторинга
        services.AddScoped<AddTenantChannelUseCase>();

        // Регистраци сценария получения профиля фирмы
        services.AddScoped<GetTenantProfileUseCase>();

        // Удаление связки фирма - топик
        services.AddScoped<RemoveTenantChannelUseCase>();

        // Получение списка всех фирм
        services.AddScoped<GetAllTenantsUseCase>();


        return services;
    }
}
