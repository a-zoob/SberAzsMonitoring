using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Configuration;

using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SberAzsMonitoring.Dashboard.Infrastructure.Persistence;

/// <summary>
/// Обеспечивает безопасное начальное заполнение (сидирование) базы данных Дашборда.
/// </summary>
public static class DashboardDbContextSeed
{
    /// <summary>
    /// Автоматически создает администратора, если таблица пуста и параметры переданы через конфигурацию.
    /// </summary>
    public static async Task SeedDefaultUserAsync(
        IDashboardDbContext dbContext,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        try
        {
            var hasUsers = await dbContext.Users.AnyAsync();
            if (hasUsers) return;

            // Извлекаем учетные данные из конфигурации (Docker Environment)
            string? seedLogin = configuration["IdentitySettings:SeedAdminLogin"];
            string? seedPassword = configuration["IdentitySettings:SeedAdminPassword"];

            // Если переменные окружения не настроены — выходим, защищая систему от хардкода
            if (string.IsNullOrWhiteSpace(seedLogin) || string.IsNullOrWhiteSpace(seedPassword))
            {
                logger.LogWarning("[DashboardSeed] Таблица пользователей пуста, но параметры 'SeedAdminLogin' или 'SeedAdminPassword' не заданы в конфигурации. Сидирование пропущено.");
                return;
            }

            logger.LogInformation("[DashboardSeed] Запуск генерации администратора на основе настроек инфраструктуры...");

            // Хэшируем безопасный пароль
            string passwordHash = passwordHasher.HashPassword(seedPassword);

            var adminUser = new DashboardUser(
                id: Guid.NewGuid(),
                login: seedLogin.Trim(),
                passwordHash: passwordHash,
                role: "Administrator"
            );

            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("[DashboardSeed] Учетная запись администратора '{Login}' успешно создана.", seedLogin);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Критическая ошибка во время сидирования базы данных Дашборда.");
            throw;
        }
    }
}
