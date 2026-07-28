using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SberAzsMonitoring.Dashboard.Domain.Entities;

namespace SberAzsMonitoring.Dashboard.Infrastructure.Persistence.Configurations;

public sealed class DashboardTenantConfiguration : IEntityTypeConfiguration<DashboardTenant>
{
    public void Configure(EntityTypeBuilder<DashboardTenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.EncryptedNtfyAccessWithValue)
            .HasColumnName("encrypted_ntfy_access_token")
            .IsRequired(false);

        builder.Property(t => t.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(t => t.Balance)
        .HasColumnName("balance")
        .HasColumnType("numeric(18,2)")
        .HasDefaultValue(0.00m)
        .IsRequired();

        // Глобальный фильтр: Дашборд по умолчанию не видит удаленные фирмы (Soft Delete)
        
        builder.HasQueryFilter(t => !t.IsDeleted);

        // Настройка связи один-ко-многим к каналам регионов
        builder.HasMany(t => t.Channels)
            .WithOne()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
