using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;

namespace SberAzsMonitoring.Dashboard.Infrastructure.Cryptography;

/// <summary>
/// Промышленная реализация криптографии AES-256 с динамическим чтением ключей из конфигурации среды.
/// </summary>
public sealed class AesDataEncryptionService : IDataEncryptionService
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _encryptionIv;

    public AesDataEncryptionService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Читаем ключи, удаляя пробелы и скрытые символы переноса строк (\r\n) по краям
        var base64Key = configuration["CryptoSettings:MasterKey"]?.Trim();
        var base64Iv = configuration["CryptoSettings:InitializationVector"]?.Trim();

        if (string.IsNullOrWhiteSpace(base64Key))
            throw new InvalidOperationException("Критическая ошибка безопасности: Переменная среды 'CryptoSettings:MasterKey' не задана.");

        if (string.IsNullOrWhiteSpace(base64Iv))
            throw new InvalidOperationException("Критическая ошибка безопасности: Переменная среды 'CryptoSettings:InitializationVector' не задана.");

        try
        {
            var rawKeyBytes = Convert.FromBase64String(base64Key);
            var rawIvBytes = Convert.FromBase64String(base64Iv);

            // ЗАЩИТА: Если Docker передал лишние байты (34 вместо 32), обрезаем массив ровно до стандартов AES
            if (rawKeyBytes.Length != 32)
            {
                _encryptionKey = new byte[32];
                Array.Copy(rawKeyBytes, _encryptionKey, Math.Min(rawKeyBytes.Length, 32));
            }
            else
            {
                _encryptionKey = rawKeyBytes;
            }

            if (rawIvBytes.Length != 16)
            {
                _encryptionIv = new byte[16];
                Array.Copy(rawIvBytes, _encryptionIv, Math.Min(rawIvBytes.Length, 16));
            }
            else
            {
                _encryptionIv = rawIvBytes;
            }
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Ошибка инициализации криптографии: Секретные ключи должны быть валидными строками Base64.", ex);
        }

        // Финальная жесткая проверка размеров ключей согласно стандарту AES-256
        if (_encryptionKey.Length != 32)
            throw new CryptographicException($"Неверная длина мастер-ключа AES. Ожидается 32 байта (256 бит), получено: {_encryptionKey.Length} байт.");

        if (_encryptionIv.Length != 16)
            throw new CryptographicException($"Неверная длина вектора инициализации (IV). Ожидается 16 байт (128 бит), получено: {_encryptionIv.Length} байт.");
    }


    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = _encryptionIv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = _encryptionIv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);

        return sr.ReadToEnd();
    }
}
