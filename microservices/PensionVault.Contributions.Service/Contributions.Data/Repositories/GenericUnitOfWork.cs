using Contributions.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Contributions.Data.Repositories;

public class GenericUnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;
    public GenericUnitOfWork(TContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}



