using System;
using System.Collections.Generic;

namespace SberAzsMonitoring.Dashboard.Domain.Entities;

/// <summary>
/// Сущность фирмы (тенанта) для администрирования на стороне Дашборда.
/// </summary>
public sealed class DashboardTenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;

    // В БД Дашборда ключ хранится строго в зашифрованном виде (строка AES-256)
    public string? EncryptedNtfyAccessWithValue { get; private set; }

    /// <summary>
    /// Текущий финансовый баланс фирмы для контроля отправки уведомлений.
    /// </summary>
    public decimal Balance { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Карта каналов (Вариант Б) для связи один-ко-многим в Entity Framework
    private readonly List<DashboardTenantChannel> _channels = new();
    public IReadOnlyCollection<DashboardTenantChannel> Channels => _channels.AsReadOnly();

    /// <summary>
    /// Системный логин фирмы для авторизации в ntfy-server по стандарту Чистой Архитектуры.
    /// </summary>
    public string SystemLogin
    {
        get
        {
            string safeName = Name.ToLowerInvariant().Replace(" ", "");
            return $"t_{safeName}_shared";
        }
    }

    // Конструктор для ORM (Entity Framework Core)
    private DashboardTenant() { }

    public DashboardTenant(Guid id, string name, string? encryptedToken, decimal initialBalance = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя фирмы не может быть пустым", nameof(name));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = name;
        EncryptedNtfyAccessWithValue = encryptedToken;
        Balance = initialBalance < 0 ? throw new ArgumentException("Начальный баланс не может быть отрицательным", nameof(initialBalance)) : initialBalance;
        IsDeleted = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Бизнес-метод обновления данных фирмы.
    /// </summary>
    public void Update(string name, string? encryptedToken, decimal balance)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя фирмы не может быть пустым", nameof(name));
        if (balance < 0)
            throw new ArgumentException("Баланс фирмы не может быть отрицательным", nameof(balance));

        Name = name;
        EncryptedNtfyAccessWithValue = encryptedToken;
        Balance = balance;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Бизнес-метод мягкого удаления фирмы (Soft Delete).
    /// </summary>
    public void Delete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveChannel(string sysTopicName)
    {
        if (string.IsNullOrWhiteSpace(sysTopicName))
        {
            throw new ArgumentException("Имя топика не может быть пустым.", nameof(sysTopicName));
        }

        // Ищем канал по свойству NtfyTopic, которое хранит системное имя топика
        var existingChannel = _channels.FirstOrDefault(c =>
            c.NtfyTopic.Equals(sysTopicName, StringComparison.OrdinalIgnoreCase));

        if (existingChannel == null)
        {
            throw new InvalidOperationException($"Фирма не подписана на регион/топик '{sysTopicName}'.");
        }

        // Удаляем из внутренней инкапсулированной коллекции
        _channels.Remove(existingChannel);
    }
}
