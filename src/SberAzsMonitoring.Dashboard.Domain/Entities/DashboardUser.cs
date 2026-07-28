using System;

namespace SberAzsMonitoring.Dashboard.Domain.Entities;

/// <summary>
/// Сущность Пользователя (Администратора) для аутентификации в Панели управления.
/// </summary>
public sealed class DashboardUser
{
    public Guid Id { get; private set; }
    public string Login { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Role { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    // Конструктор для ORM Entity Framework Core
    private DashboardUser() { }

    public DashboardUser(Guid id, string login, string passwordHash, string role = "Administrator")
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException("Логин не может быть пустым.", nameof(login));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Хэш пароля не может быть пустым.", nameof(passwordHash));
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Роль не может быть пустой.", nameof(role));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Login = login.Trim();
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }
}
