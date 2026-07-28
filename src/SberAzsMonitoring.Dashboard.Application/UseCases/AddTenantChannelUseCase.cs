using Microsoft.EntityFrameworkCore;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Application.Interfaces;
using SberAzsMonitoring.Dashboard.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.Dashboard.Application.UseCases;

public sealed class AddTenantChannelUseCase
{
    private readonly IDashboardDbContext _dbContext;
    private readonly INtfyAuthService _ntfyAuthService;

    public AddTenantChannelUseCase(
        IDashboardDbContext dbContext,
        INtfyAuthService ntfyAuthService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _ntfyAuthService = ntfyAuthService ?? throw new ArgumentNullException(nameof(ntfyAuthService));
    }

    public async Task ExecuteAsync(Guid tenantId, string sysTopicName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sysTopicName))
        {
            throw new ArgumentException("Системное имя топика не может быть пустым.", nameof(sysTopicName));
        }

        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant == null)
        {
            throw new InvalidOperationException($"Фирма с идентификатором {tenantId} не найдена в системе.");
        }

        var alreadyExists = await _dbContext.TenantChannels
            .AnyAsync(c => c.TenantId == tenantId && c.NtfyTopic == sysTopicName, cancellationToken);

        if (alreadyExists)
        {
            throw new InvalidOperationException($"Фирма '{tenant.Name}' уже имеет активную подписку на топик '{sysTopicName}'.");
        }

        // ИСТОЧНИК ПРАВДЫ: Вызываем только выдачу прав (GrantAccessAsync) на топик.
        // Метод RegisterUserAsync и tenant.Update здесь больше не вызываются.
        // Это предотвращает перезапись токена при добавлении последующих каналов.
        await _ntfyAuthService.GrantAccessAsync(tenant.SystemLogin, sysTopicName, cancellationToken);

        // Конструируем и сохраняем запись канала в PostgreSQL
        var newChannel = new DashboardTenantChannel(tenantId, sysTopicName, sysTopicName);
        await _dbContext.TenantChannels.AddAsync(newChannel, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
