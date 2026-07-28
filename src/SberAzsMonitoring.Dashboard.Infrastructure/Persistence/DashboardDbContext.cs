using Microsoft.EntityFrameworkCore;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Domain.Entities;
using System.Reflection;

namespace SberAzsMonitoring.Dashboard.Infrastructure.Persistence;

public sealed class DashboardDbContext : DbContext, IDashboardDbContext
{
    public DbSet<DashboardTenant> Tenants => Set<DashboardTenant>();
    public DbSet<DashboardTenantChannel> TenantChannels => Set<DashboardTenantChannel>();

    /// <summary>
    /// Таблица пользователей (администраторов) панели управления в PostgreSQL.
    /// </summary>
    public DbSet<DashboardUser> Users => Set<DashboardUser>();

    public DashboardDbContext(DbContextOptions<DashboardDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Включаем расширение для генерации UUID в PostgreSQL
        modelBuilder.HasPostgresExtension("uuid-ossp");

        // Автоматически находим и применяем все конфигурации (IEntityTypeConfiguration), 
        // включая конфигурацию для DashboardUser из этой же сборки
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
