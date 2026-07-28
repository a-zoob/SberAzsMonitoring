namespace SberAzsMonitoring.NotificationWorker.Domain.Entities;

/// <summary>
/// Сущность Фирмы (Тенанта) корпоративного уровня. Хранение ключей.
/// </summary>
public sealed class Tenant
{
    public string Id { get; }
    public string Name { get; }
    public string NtfyAccessToken { get; }
    public DateTime OffsetUpdatedAt { get; } // Для контроля актуальности состояния

    // Вариант Б: Регион -> Топик ntfy
    private readonly Dictionary<string, string> _regionChannels;

    public Tenant(
        string id,
        string name,
        string ntfyAccessToken,
        Dictionary<string, string> regionChannels,
        DateTime offsetUpdatedAt)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id не может быть пустым", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Имя не может быть пустым", nameof(name));

        Id = id;
        Name = name;
        NtfyAccessToken = ntfyAccessToken;
        _regionChannels = regionChannels ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        OffsetUpdatedAt = offsetUpdatedAt;
    }

    public bool IsSubscribedToRegion(string regionName) => _regionChannels.ContainsKey(regionName);

    public string? GetNtfyTopicForRegion(string regionName)
    {
        return _regionChannels.TryGetValue(regionName, out var topic) ? topic : null;
    }
}
