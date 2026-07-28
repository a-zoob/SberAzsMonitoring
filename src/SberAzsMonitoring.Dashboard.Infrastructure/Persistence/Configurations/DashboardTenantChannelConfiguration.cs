using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SberAzsMonitoring.Dashboard.Domain.Entities;

namespace SberAzsMonitoring.Dashboard.Infrastructure.Persistence.Configurations;

public sealed class DashboardTenantChannelConfiguration : IEntityTypeConfiguration<DashboardTenantChannel>
{
    public void Configure(EntityTypeBuilder<DashboardTenantChannel> builder)
    {
        builder.ToTable("tenant_region_channels");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(c => c.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(c => c.RegionName)
            .HasColumnName("region_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.NtfyTopic)
            .HasColumnName("ntfy_topic")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        // Уникальный индекс: У одной фирмы не может быть дублирующихся топиков на один и тот же регион
        builder.HasIndex(c => new { c.TenantId, c.RegionName })
            .IsUnique()
            .HasDatabaseName("uq_tenant_region");

        // Индексы для оптимизации поиска по региону
        builder.HasIndex(c => c.RegionName)
            .HasDatabaseName("idx_tenant_channels_region_name");
    }
}
