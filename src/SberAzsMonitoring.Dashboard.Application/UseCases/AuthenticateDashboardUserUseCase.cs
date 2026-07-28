using Microsoft.EntityFrameworkCore;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.Dashboard.Application.UseCases;

/// <summary>
/// Бизнес-сценарий аутентификации администратора в Панели управления.
/// </summary>
public sealed class AuthenticateDashboardUserUseCase
{
    private readonly IDashboardDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public AuthenticateDashboardUserUseCase(IDashboardDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    /// <summary>
    /// Выполняет проверку учетных данных пользователя.
    /// </summary>
    /// <param name="login">Введенный логин.</param>
    /// <param name="rawPassword">Введенный пароль в открытом виде.</param>
    /// <returns>Флаг успешности аутентификации.</returns>
    public async Task<bool> ExecuteAsync(string login, string rawPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(rawPassword))
        {
            return false;
        }

        var normalizedLogin = login.Trim();

        // 1. Ищем пользователя в базе данных PostgreSQL по логину
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Login == normalizedLogin, cancellationToken);

        if (user == null)
        {
            // Пользователь не найден
            return false;
        }

        // 2. Сверяем хэш введенного пароля с хэшем из БД за фиксированное время (Timing Attack Protection)
        bool isPasswordValid = _passwordHasher.VerifyPassword(rawPassword, user.PasswordHash);

        return isPasswordValid;
    }
}
