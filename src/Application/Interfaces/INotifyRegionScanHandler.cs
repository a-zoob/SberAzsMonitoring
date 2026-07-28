using System.Threading;
using System.Threading.Tasks;
using SberAzsMonitoring.Application.Common.DTOs; // Для доступа к NotifyResultDto
using SberAzsMonitoring.Application.Features.Commands; // Для доступа к NotifyRegionScanCommand

namespace SberAzsMonitoring.Application.Interfaces;

/// <summary>
/// Интерфейс обработчика команды внеочередного сканирования региона.
/// </summary>
public interface INotifyRegionScanHandler
{
    /// <summary>
    /// Асинхронно обрабатывает команду сканирования и отправляет результаты.
    /// </summary>
    /// <param name="command">Объект команды с метаданными (включая TargetTenantId).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task<NotifyResultDto> HandleAsync(NotifyRegionScanCommand command, CancellationToken cancellationToken = default);
}
