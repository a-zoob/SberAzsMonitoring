namespace SberAzsMonitoring.Dashboard.Application.Common.Interfaces;

/// <summary>
/// Интерфейс криптографической службы для защиты чувствительных данных (токенов).
/// </summary>
public interface IDataEncryptionService
{
    /// <summary>
    /// Шифрует открытый текст токена для безопасного хранения в БД.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Расшифровывает зашифрованный токен для передачи в доверенную среду (Воркер через Kafka).
    /// </summary>
    string Decrypt(string cipherText);
}
