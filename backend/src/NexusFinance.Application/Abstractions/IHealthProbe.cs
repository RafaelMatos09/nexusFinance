namespace NexusFinance.Application.Abstractions;

public interface IHealthProbe
{
    Task<bool> DatabaseIsReachableAsync(CancellationToken cancellationToken = default);
}
