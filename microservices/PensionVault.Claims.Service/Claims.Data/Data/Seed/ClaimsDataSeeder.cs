using Claims.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Claims.Data;

namespace Claims.Data.Seed;

public static class ClaimsDataSeeder
{
    public static async Task SeedAsync(ClaimsDbContext context)
    {
        // No default claims are seeded in monolith, but we can seed if we want or just do check
        await Task.CompletedTask;
    }
}



