using Members.Domain.Entities;
namespace Members.Domain.Repositories;

public interface IClaimRepository
{
    Task<List<ExternalClaim>> GetAllAsync();
}


