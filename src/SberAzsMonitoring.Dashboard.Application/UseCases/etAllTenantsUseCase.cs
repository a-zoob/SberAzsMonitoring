using Microsoft.EntityFrameworkCore;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;

namespace SberAzsMonitoring.Dashboard.Application.UseCases;

// Легковесный контракт для вывода в общую таблицу на фронтенде
public record TenantLookupDto(
    Guid Id,
    string Name,
    decimal Balance,
    int ActiveChannelsCount
);

public class GetAllTenantsUseCase
{
    private readonly IDashboardDbContext _dbContext;

    public GetAllTenantsUseCase(IDashboardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TenantLookupDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Используем AsNoTracking() для максимальной производительности, так как это операция только на чтение.
        // Проектируем (Select) данные прямо на уровне SQL-запроса, чтобы не выкачивать лишние поля.
        return await _dbContext.Tenants
            .AsNoTracking()
            .Select(t => new TenantLookupDto(
                t.Id,
                t.Name,
                t.Balance,
                t.Channels.Count // Агрегируем количество каналов без загрузки всей коллекции объектов
            ))
            .ToListAsync(cancellationToken);
    }
}
