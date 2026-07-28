using Microsoft.EntityFrameworkCore;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Application.Interfaces;
using SberAzsMonitoring.Dashboard.Domain.Entities;
using System;
using System;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.Dashboard.Application.UseCases;

public sealed record TenantCredentialsResponse(string Login, string AccessToken);

public sealed class GetTenantCredentialsUseCase
{
    private readonly IDashboardDbContext _dbContext;
    private readonly IDataEncryptionService _encryptionService;

    public GetTenantCredentialsUseCase(
        IDashboardDbContext dbContext,
        IDataEncryptionService encryptionService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    public async Task<TenantCredentialsResponse> ExecuteAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        // 1. Извлекаем фирму из промышленного датасета Tenants
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant == null)
        {
            throw new InvalidOperationException($"Фирма с идентификатором {tenantId} не найдена в системе.");
        }

        if (string.IsNullOrEmpty(tenant.EncryptedNtfyAccessWithValue))
        {
            throw new InvalidOperationException("Для данной фирмы еще не сгенерированы ключи доступа. Сначала привяжите ее к региону.");
        }

        // 2. Дешифруем токен с помощью реального промышленного крипто-моста системы
        string decryptedToken = _encryptionService.Decrypt(tenant.EncryptedNtfyAccessWithValue);

        // 3. Формируем логин по правилам генерации Дашборда
        string safeTenantName = tenant.Name.ToLowerInvariant().Replace(" ", "");
        string generatedLogin = $"t_{safeTenantName}_shared";

        return new TenantCredentialsResponse(generatedLogin, decryptedToken);
    }
}
