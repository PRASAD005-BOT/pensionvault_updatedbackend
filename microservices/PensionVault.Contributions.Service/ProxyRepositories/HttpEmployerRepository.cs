using Microsoft.AspNetCore.Http;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Contributions.Service.ProxyRepositories;

public class HttpEmployerRepository : BaseHttpRepository, IEmployerRepository
{
    public HttpEmployerRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public Task<Employer?> FindByIdAsync(Guid employerId)
        => GetAsync<Employer>($"api/employers/{employerId}");

    public async Task<List<Employer>> GetAllAsync()
        => await GetAsync<List<Employer>>("api/employers") ?? new List<Employer>();

    public Task<bool> ExistsByRegistrationNumberAsync(string registrationNumber) => throw new NotSupportedException();
    public Task AddAsync(Employer employer) => throw new NotSupportedException();
}

