using System;
using System.Security.Cryptography;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;

namespace SberAzsMonitoring.Dashboard.Infrastructure.Cryptography;

/// <summary>
/// Промышленная реализация хэширования паролей по стандарту PBKDF2 (SHA512, 100 000 итераций).
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 бит
    private const int KeySize = 32;  // 256 бит
    private const int Iterations = 100_000; // Рекомендованный стандарт безопасности
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

    public string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        // 1. Генерируем криптографически стойкую соль
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        // 2. Вычисляем хэш пароля через корректный статический метод Pbkdf2
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySize);

        // 3. Формируем итоговый массив: маркер версии, [1..16] соль, [17..48] хэш
        byte[] result = new byte[1 + SaltSize + KeySize];
        result[0] = 0x01; // Версия формата хэша
        Buffer.BlockCopy(salt, 0, result, 1, SaltSize);
        Buffer.BlockCopy(hash, 0, result, 1 + SaltSize, KeySize);

        return Convert.ToBase64String(result);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (string.IsNullOrWhiteSpace(passwordHash)) return false;

        try
        {
            byte[] decodedHashBytes = Convert.FromBase64String(passwordHash);

            // Проверка длины и байта версии
            if (decodedHashBytes.Length != (1 + SaltSize + KeySize) || decodedHashBytes[0] != 0x01)
            {
                return false;
            }

            // Выделяем соль
            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(decodedHashBytes, 1, salt, 0, SaltSize);

            // Выделяем эталонный хэш
            byte[] expectedHash = new byte[KeySize];
            Buffer.BlockCopy(decodedHashBytes, 1 + SaltSize, expectedHash, 0, KeySize);

            // Вычисляем хэш проверяемого пароля с той же солью через корректный статический метод Pbkdf2
            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySize);

            // Сравниваем массивы за фиксированное время для защиты от атак по времени (Timing Attacks)
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
