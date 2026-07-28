using Microsoft.EntityFrameworkCore;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Application.Interfaces;
using SberAzsMonitoring.Dashboard.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.Dashboard.Application.UseCases;

public sealed record TenantChannelDto(Guid Id, string RegionName, string NtfyTopic, DateTime CreatedAt);
public sealed record TenantProfileResponse(Guid Id, string Name, decimal Balance, List<TenantChannelDto> Channels);

public sealed class GetTenantProfileUseCase
{
    private readonly IDashboardDbContext _dbContext;

    public GetTenantProfileUseCase(IDashboardDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<TenantProfileResponse> ExecuteAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant == null)
        {
            throw new InvalidOperationException($"Фирма с идентификатором {tenantId} не найдена.");
        }

        // Вычитываем активные каналы подписок этой фирмы через точное свойство интерфейса
        var channels = await _dbContext.TenantChannels
            .Where(c => c.TenantId == tenantId)
            .Select(c => new TenantChannelDto(c.Id, c.RegionName, c.NtfyTopic, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return new TenantProfileResponse(
            tenant.Id,
            tenant.Name,
            tenant.Balance,
            channels
        );
    }
}
