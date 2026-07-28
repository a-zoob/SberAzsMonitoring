using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SberAzsMonitoring.Dashboard.Domain.Entities;

namespace SberAzsMonitoring.Dashboard.Infrastructure.Persistence.Configurations;

public sealed class DashboardUserConfiguration : IEntityTypeConfiguration<DashboardUser>
{
    public void Configure(EntityTypeBuilder<DashboardUser> builder)
    {
        builder.ToTable("dashboard_users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(u => u.Login)
            .HasColumnName("login")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasMaxLength(50)
            .HasDefaultValue("Administrator")
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        // Уникальный индекс для логина, чтобы исключить дублирование пользователей
        builder.HasIndex(u => u.Login)
            .IsUnique()
            .HasDatabaseName("uq_dashboard_users_login");
    }
}
