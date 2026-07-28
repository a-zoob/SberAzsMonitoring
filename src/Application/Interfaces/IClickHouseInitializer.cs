namespace SberAzsMonitoring.Application.Interfaces;

public interface IClickHouseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
