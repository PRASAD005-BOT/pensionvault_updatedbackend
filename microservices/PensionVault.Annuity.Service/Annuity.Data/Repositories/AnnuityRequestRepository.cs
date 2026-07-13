using Annuity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Annuity.Domain.Entities;
using Annuity.Data;

namespace Annuity.Data.Repositories;

public class AnnuityRequestRepository : IAnnuityRequestRepository
{
    private readonly AnnuityDbContext _context;
    public AnnuityRequestRepository(AnnuityDbContext context) => _context = context;

    public async Task AddAsync(AnnuityRequest request)
        => await _context.AnnuityRequests.AddAsync(request);

    public Task<AnnuityRequest?> FindByIdAsync(Guid requestId)
        => _context.AnnuityRequests
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

    public Task<List<AnnuityRequest>> GetByMemberAsync(Guid memberId)
        => _context.AnnuityRequests
            .Where(r => r.MemberId == memberId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public Task<List<AnnuityRequest>> GetPendingAsync()
        => _context.AnnuityRequests
            .Where(r => r.Status == AnnuityRequestStatus.Pending)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public Task<List<AnnuityRequest>> GetAllAsync()
        => _context.AnnuityRequests
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public Task<AnnuityRequest?> FindPendingByMemberAsync(Guid memberId)
        => _context.AnnuityRequests
            .FirstOrDefaultAsync(r => r.MemberId == memberId && r.Status == AnnuityRequestStatus.Pending);
}




