using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Application.Interfaces;
using SberAzsMonitoring.Dashboard.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SberAzsMonitoring.Dashboard.Application.UseCases;

public sealed class CreateTenantUseCase
{
    private readonly IDashboardDbContext _dbContext;
    private readonly INtfyAuthService _ntfyAuthService;
    private IDataEncryptionService _encryptionService;
    public CreateTenantUseCase(
        IDashboardDbContext dbContext,
        INtfyAuthService ntfyAuthService,
        IDataEncryptionService encryptionService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _ntfyAuthService = ntfyAuthService ?? throw new ArgumentNullException(nameof(ntfyAuthService));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    public async Task<Guid> ExecuteAsync(string name, string rawNtfyToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название фирмы не может быть пустым.", nameof(name));
        }

        // 1. Создаем доменный объект фирмы (инициализация в памяти)
        var tenantId = Guid.NewGuid();
        // Передаем пустую строку в качестве временного encryptedToken, так как он обновится методом ниже
        var tenant = new DashboardTenant(tenantId, name, string.Empty);

        // 2. Вызываем МЕТОД 1: Создаем пользователя в ntfy-server СТРОГО ОДИН РАЗ при рождении фирмы.
        // Передаем автоматически сгенерированный системой SystemLogin фирмы.
        string generatedNtfyPassword = await _ntfyAuthService.RegisterUserAsync(tenant.SystemLogin, cancellationToken);

       //3.Сохраняем сгенерированный стабильный пароль в зашифрованное поле агрегата
        // Сначала шифруем чистый пароль через промышленный крипто-сервис Дашборда
        string encryptedPassword = _encryptionService.Encrypt(generatedNtfyPassword);

        // Передаем в доменный метод ЗАШИФРОВАННЫЙ хэш для записи в PostgreSQL
        tenant.Update(tenant.Name, encryptedPassword, tenant.Balance);

        // 4. Записываем запись в PostgreSQL
        await _dbContext.Tenants.AddAsync(tenant, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return tenantId;
    }
}
