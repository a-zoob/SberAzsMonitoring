//проверяем логику работы api и сериализации, не зависимо от наполненности БД
//поэтому динамически подменяем контракт IClickHouseAnalyticsRepository на заглушку Mock-репозиторий.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces.Analytics;

namespace SberAzsMonitoring.Dashboard.IntegrationTests;

public class DashboardWebApplicationFactory : WebApplicationFactory<Program>
{
    // Предоставляем тестам доступ к Mock-объекту, чтобы настраивать фейковые данные
    public Mock<IClickHouseAnalyticsRepository> ClickHouseRepositoryMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Находим и удаляем реальную регистрацию репозитория ClickHouse, сделанную в Infrastructure
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IClickHouseAnalyticsRepository));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Внедряем Mock-объект вместо реальной базы данных
            services.AddScoped(_ => ClickHouseRepositoryMock.Object);
        });
    }
}
