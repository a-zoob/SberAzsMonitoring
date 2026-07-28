using Microsoft.EntityFrameworkCore;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Application.Interfaces;

namespace SberAzsMonitoring.Dashboard.Application.UseCases;

public class RemoveTenantChannelUseCase
{
    private readonly IDashboardDbContext _dbContext;
    private readonly INtfyAuthService _ntfyAuthService;
    private readonly ITenantConfigurationPublisher _configPublisher;

    public RemoveTenantChannelUseCase(
        IDashboardDbContext dbContext,
        INtfyAuthService ntfyAuthService,
        ITenantConfigurationPublisher configPublisher)
    {
        _dbContext = dbContext;
        _ntfyAuthService = ntfyAuthService;
        _configPublisher = configPublisher;
    }

    public async Task ExecuteAsync(Guid tenantId, string sysTopicName, CancellationToken cancellationToken = default)
    {
        // 1. Извлекаем сущность тенанта вместе с его активными каналами (подписками)
        var tenant = await _dbContext.Tenants
            .Include(t => t.Channels)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Фирма с идентификатором {tenantId} не найдена.");
        }

        // 2. Вызываем доменную логику валидации и удаления канала из коллекции
        tenant.RemoveChannel(sysTopicName);

        // 3. Фиксируем каскадное удаление строки из PostgreSQL
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Инфраструктурный шаг: Лишаем фирму прав "read" на топик в ntfy-server (ACL)
        // Будет реализован в NtfyAuthService на следующем шаге
        await _ntfyAuthService.RevokeAccessAsync(tenant.SystemLogin, sysTopicName, cancellationToken);

        // 5. Инфраструктурный шаг: Публикуем обновленный snapshot фирмы в Kafka топик tenant-configuration-events
        // Воркеры мгновенно перестроят свой In-Memory кэш
        await _configPublisher.PublishUpdateAsync(tenant, cancellationToken);
    }
}
