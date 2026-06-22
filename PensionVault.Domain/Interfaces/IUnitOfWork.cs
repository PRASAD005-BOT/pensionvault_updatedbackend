namespace PensionVault.Domain.Interfaces;

/// <summary>
/// Wraps SaveChangesAsync so services can commit multiple repository operations atomically.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
