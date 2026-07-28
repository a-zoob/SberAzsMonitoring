using SberAzsMonitoring.Domain;

namespace SberAzsMonitoring.Application.Interfaces;

public interface IFuelParserService
{
    Task<IEnumerable<FuelStation>> ParseActualPricesAsync(CancellationToken cancellationToken = default);
}

