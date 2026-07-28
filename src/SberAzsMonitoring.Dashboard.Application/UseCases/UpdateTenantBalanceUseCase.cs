using Microsoft.EntityFrameworkCore;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.Dashboard.Application.UseCases;

/// <summary>
/// Бизнес-сценарий корректировки / пополнения баланса фирмы-контрагента.
/// </summary>
public sealed class UpdateTenantBalanceUseCase
{
    private readonly IDashboardDbContext _dbContext;

    public UpdateTenantBalanceUseCase(IDashboardDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Изменяет баланс существующей активной фирмы по её идентификатору.
    /// </summary>
    public async Task ExecuteAsync(Guid tenantId, decimal newBalance, CancellationToken cancellationToken = default)
    {
        if (newBalance < 0)
            throw new ArgumentException("Баланс фирмы не может быть отрицательным.", nameof(newBalance));

        // 1. Ищем активную фирму в PostgreSQL
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant == null)
        {
            throw new InvalidOperationException($"Фирма с идентификатором '{tenantId}' не найдена или удалена.");
        }

        // 2. Используем точный доменный метод вашей модели для безопасного изменения баланса
        // Передаем текущее имя, текущий зашифрованный токен и новый баланс
        tenant.Update(tenant.Name, tenant.EncryptedNtfyAccessWithValue, newBalance);

        // 3. Сохраняем изменения в базу данных
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
