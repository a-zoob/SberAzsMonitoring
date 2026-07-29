using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SberAzsMonitoring.Dashboard.Application.Interfaces; // Подключаем интерфейс сервиса авторизации
using SberAzsMonitoring.Dashboard.Infrastructure.Services; // Подключаем реализацию сервиса авторизации
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Infrastructure.Cryptography;
using SberAzsMonitoring.Dashboard.Infrastructure.Messaging;
using SberAzsMonitoring.Dashboard.Infrastructure.Persistence;
using System;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces.Analytics;
using SberAzsMonitoring.Dashboard.Infrastructure.Services.Analytics;

namespace SberAzsMonitoring.Dashboard.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует инфраструктурные сервисы подсистемы Дашборда в контейнере зависимостей.
    /// </summary>
    public static IServiceCollection AddDashboardInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Извлекаем строку подключения к PostgreSQL
        string? connectionString = configuration.GetConnectionString("DashboardDb");
        services.AddDbContext<DashboardDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Мост инверсии зависимостей для контекста БД
        services.AddScoped<IDashboardDbContext>(provider =>
            provider.GetRequiredService<DashboardDbContext>());

        // 2. Внедрение фабрики хэширования паролей пользователей
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IDataEncryptionService, AesDataEncryptionService>();

        // Регистрация службы публикации конфигураций фирм в шину брокера Kafka
        services.AddScoped<ITenantConfigurationPublisher, KafkaTenantConfigurationPublisher>();

        // 3. Настройка пула HttpClient для службы авторизации ntfy (Clean Code)
        services.AddHttpClient<INtfyAuthService, NtfyAuthService>(client =>
        {
            string baseUrlStr = Environment.GetEnvironmentVariable("RegionSettings__NtfyBaseUrl") ?? "http://ntfy-server";
            client.BaseAddress = new Uri(baseUrlStr);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // 4. Регистрация репозитория аналитики ClickHouse
        services.AddScoped<IClickHouseAnalyticsRepository, ClickHouseAnalyticsRepository>();

        return services;
    }
}
