using Microsoft.EntityFrameworkCore;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.Dashboard.Application.UseCases;

public sealed class CreateDashboardUserUseCase
{
    private readonly IDashboardDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public CreateDashboardUserUseCase(IDashboardDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    /// <summary>
    /// Выполняет бизнес-сценарий создания и регистрации нового администратора Дашборда.
    /// </summary>
    public async Task ExecuteAsync(
        string login,
        string rawPassword,
        string role = "Administrator",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException("Логин пользователя не может быть пустым.", nameof(login));
        if (string.IsNullOrWhiteSpace(rawPassword))
            throw new ArgumentException("Пароль не может быть пустым.", nameof(rawPassword));

        var normalizedLogin = login.Trim();

        // Валидация уникальности логина на уровне бизнес-логики
        var isLoginBusy = await _dbContext.Users
            .AnyAsync(u => u.Login == normalizedLogin, cancellationToken);

        if (isLoginBusy)
        {
            throw new InvalidOperationException($"Пользователь с логином '{normalizedLogin}' уже зарегистрирован в системе.");
        }

        // Хэширование пароля через абстрактную службу
        string passwordHash = _passwordHasher.HashPassword(rawPassword);

        // Создание доменной сущности
        var newUser = new DashboardUser(Guid.NewGuid(), normalizedLogin, passwordHash, role);

        // Сохранение в базу данных
        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
