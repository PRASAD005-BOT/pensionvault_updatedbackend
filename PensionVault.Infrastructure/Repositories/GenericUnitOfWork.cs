using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Infrastructure.Repositories;

public class GenericUnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;
    public GenericUnitOfWork(TContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
