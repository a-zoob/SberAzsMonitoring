using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using System;
using System.IO;

namespace SberAzsMonitoring.Dashboard.Infrastructure.Persistence;

/// <summary>
/// Истинно чистая фабрика контекста. 
/// Извлекает настройки СУБД напрямую из корневого docker-compose.yml, исключая дублирование данных.
/// </summary>
public sealed class DashboardDbContextFactory : IDesignTimeDbContextFactory<DashboardDbContext>
{
    public DashboardDbContext CreateDbContext(string[] args)
    {
        // 1. Находим корневой каталог решения, где гарантированно лежит docker-compose.yml
        string rootPath = Directory.GetCurrentDirectory();
        string composePath = Path.Combine(rootPath, "docker-compose.yml");

        // Корректировка пути, если команда запущена из глубокой подпапки
        if (!File.Exists(composePath))
        {
            rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..");
            composePath = Path.Combine(rootPath, "docker-compose.yml");
        }

        if (!File.Exists(composePath))
        {
            throw new FileNotFoundException($"Критическая ошибка сборки: Манифест инфраструктуры не найден по пути: {composePath}");
        }

        // 2. Читаем структуру docker-compose как конфигурационный граф
        var composeConfiguration = new ConfigurationBuilder()
            .SetBasePath(rootPath)
            .AddYamlFile("docker-compose.yml", optional: false)
            .Build();

        // 3. Извлекаем строку подключения из объявленных параметров СУБД или закомментированного дашборда.
        // В соответствии с вашим файлом compose, значение лежит в блоке окружения sberazs-postgres
        string? dbName = composeConfiguration["services:sberazs-postgres:environment:POSTGRES_DB"];
        string? dbUser = composeConfiguration["services:sberazs-postgres:environment:POSTGRES_USER"];
        string? dbPass = composeConfiguration["services:sberazs-postgres:environment:POSTGRES_PASSWORD"];

        if (string.IsNullOrWhiteSpace(dbPass) || string.IsNullOrWhiteSpace(dbUser) || string.IsNullOrWhiteSpace(dbName))
        {
            throw new InvalidOperationException(
                "Не удалось извлечь параметры POSTGRES_USER, POSTGRES_PASSWORD или POSTGRES_DB из sberazs-postgres в docker-compose.yml");
        }

        // 4. Формируем строку подключения для Windows-терминала хоста (обращаемся к WSL через localhost)
        string connectionString = $"Host=localhost;Port=5432;Database={dbName};Username={dbUser};Password={dbPass};";

        var optionsBuilder = new DbContextOptionsBuilder<DashboardDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DashboardDbContext(optionsBuilder.Options);
    }
}
