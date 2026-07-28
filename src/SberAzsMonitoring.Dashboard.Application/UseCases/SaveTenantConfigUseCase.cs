using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SberAzsMonitoring.Dashboard.Application.UseCases;

public sealed class SaveTenantConfigUseCase
{
    private readonly IDashboardDbContext _dbContext;
    private readonly IDataEncryptionService _encryptionService;
    private readonly ITenantConfigurationPublisher _configPublisher;

    public SaveTenantConfigUseCase(
        IDashboardDbContext dbContext,
        IDataEncryptionService encryptionService,
        ITenantConfigurationPublisher configPublisher)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _configPublisher = configPublisher;
    }

    /// <summary>
    /// Выполняет сквозной процесс сохранения конфигурации фирмы в БД и отправку события в шину Kafka.
    /// </summary>
    public async Task ExecuteAsync(
        Guid tenantId,
        string name,
        string? rawNtfyToken,
        Dictionary<string, string> regionChannels,
        CancellationToken cancellationToken = default)
    {
        // 1. Шифруем приватный токен перед отправкой в БД (Промышленный стандарт)
        string? encryptedToken = !string.IsNullOrWhiteSpace(rawNtfyToken)
            ? _encryptionService.Encrypt(rawNtfyToken)
            : null;

        // 2. Ищем существующую фирму, включая её каналы (Вариант Б)
        var tenant = await _dbContext.Tenants
            .Include(t => t.Channels)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
        {
            // Если фирмы нет — создаем новую
            tenant = new DashboardTenant(tenantId, name, encryptedToken);
            _dbContext.Tenants.Add(tenant);
        }
        else
        {
            // Если есть — обновляем основные данные
            tenant.Update(name, encryptedToken, tenant.Balance);
        }

        // 3. Синхронизируем каналы регионов в БД (Вариант Б)
        // Удаляем те регионы, которых больше нет в новой конфигурации
        var regionsToRemove = tenant.Channels
            .Where(c => !regionChannels.ContainsKey(c.RegionName))
            .ToList();

        foreach (var channel in regionsToRemove)
        {
            _dbContext.TenantChannels.Remove(channel);
        }

        // Добавляем или обновляем топики для переданных регионов
        foreach (var (region, topic) in regionChannels)
        {
            var existingChannel = tenant.Channels
                .FirstOrDefault(c => c.RegionName.Equals(region, StringComparison.OrdinalIgnoreCase));

            if (existingChannel == null)
            {
                var newChannel = new DashboardTenantChannel(tenant.Id, region, topic);
                _dbContext.TenantChannels.Add(newChannel);
            }
            else if (!existingChannel.NtfyTopic.Equals(topic, StringComparison.Ordinal))
            {
                existingChannel.ChangeTopic(topic);
            }
        }

        // 4. Атомарно сохраняем изменения в реляционную БД PostgreSQL
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 5. Публикуем событие в Kafka. 
        // Метод PublishUpdateAsync сам перезапросит Channels или мы передадим объект,
        // но так как EF отслеживает связи (Navigation Properties), коллекция tenant.Channels уже содержит актуальные данные.
        await _configPublisher.PublishUpdateAsync(tenant, cancellationToken);
    }
}
