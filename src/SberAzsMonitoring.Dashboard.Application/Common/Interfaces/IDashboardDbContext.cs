using Microsoft.EntityFrameworkCore;
using SberAzsMonitoring.Dashboard.Domain.Entities;

namespace SberAzsMonitoring.Dashboard.Application.Common.Interfaces;

/// <summary>
/// Абстрактный контекст базы данных Дашборда для слоя бизнес-логики.
/// </summary>
public interface IDashboardDbContext
{
    DbSet<DashboardTenant> Tenants { get; }
    DbSet<DashboardTenantChannel> TenantChannels { get; }

    /// <summary>
    /// Коллекция пользователей (администраторов) панели управления.
    /// </summary>
    DbSet<DashboardUser> Users { get; }

    /// <summary>
    /// Асинхронно сохраняет все изменения, сделанные в контексте, в базу данных.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
