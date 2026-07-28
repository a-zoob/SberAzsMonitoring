namespace SberAzsMonitoring.Dashboard.Application.Common.Interfaces;

/// <summary>
/// Интерфейс службы хэширования паролей для защиты учетных записей.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Создает криптографически стойкий хэш из открытого текста пароля.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Проверяет соответствие открытого текста пароля ранее созданному хэшу.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}
