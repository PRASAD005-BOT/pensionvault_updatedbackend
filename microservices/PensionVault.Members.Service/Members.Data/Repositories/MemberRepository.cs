using Members.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Members.Domain.Entities;
using Members.Data;

namespace Members.Data.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly MembersDbContext _context;
    public MemberRepository(MembersDbContext context) => _context = context;

    public Task<Member?> FindByIdAsync(Guid memberId)
        => _context.Members
            .Include(m => m.Employer)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.MemberId == memberId);

    public Task<Member?> FindByUserIdAsync(Guid userId)
        => _context.Members
            .Include(m => m.Employer)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == userId);

    public Task<List<Member>> GetAllAsync(Guid? employerId = null)
    {
        var query = _context.Members.Include(m => m.Employer).AsQueryable();
        if (employerId.HasValue)
            query = query.Where(m => m.EmployerId == employerId.Value);
        return query.ToListAsync();
    }

    public Task<bool> ExistsByMembershipNumberAsync(string membershipNumber, Guid? excludeId = null)
    {
        var query = _context.Members.Where(m => m.MembershipNumber == membershipNumber);
        if (excludeId.HasValue)
            query = query.Where(m => m.MemberId != excludeId.Value);
        return query.AnyAsync();
    }

    public Task<bool> ExistsByUserIdAsync(Guid userId)
        => _context.Members.AnyAsync(m => m.UserId == userId);

    public async Task AddAsync(Member member)
        => await _context.Members.AddAsync(member);
}




