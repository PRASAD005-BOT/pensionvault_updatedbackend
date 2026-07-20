using Contributions.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Contributions.Domain.Entities;

namespace Contributions.Data.Repositories;

public class ShortfallRequestRepository : IShortfallRequestRepository
{
    private readonly ContributionsDbContext _context;
    public ShortfallRequestRepository(ContributionsDbContext context) => _context = context;

    public Task<ShortfallRequest?> FindByIdAsync(Guid shortfallRequestId)
        => _context.ShortfallRequests.FirstOrDefaultAsync(s => s.ShortfallRequestId == shortfallRequestId);

    public Task<List<ShortfallRequest>> GetAllAsync()
        => _context.ShortfallRequests.OrderByDescending(s => s.RaisedDate).ToListAsync();

    public Task<List<ShortfallRequest>> GetByEmployerAsync(Guid employerId)
        => _context.ShortfallRequests
            .Where(s => s.EmployerId == employerId)
            .OrderByDescending(s => s.RaisedDate)
            .ToListAsync();

    public Task<List<ShortfallRequest>> GetByMemberAsync(Guid memberId)
        => _context.ShortfallRequests
            .Where(s => s.MemberId == memberId)
            .OrderByDescending(s => s.RaisedDate)
            .ToListAsync();

    public async Task AddAsync(ShortfallRequest request)
        => await _context.ShortfallRequests.AddAsync(request);
}
