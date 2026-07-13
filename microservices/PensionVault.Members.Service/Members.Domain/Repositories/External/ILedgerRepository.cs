using Members.Domain.Entities;
namespace Members.Domain.Repositories;

public interface ILedgerRepository
{
    Task<List<ExternalLedgerEntry>> GetByAccountAsync(Guid accountId);
}


