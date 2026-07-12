using Microsoft.AspNetCore.Http;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Annuity.Service.ProxyRepositories;

public class HttpMemberRepository : BaseHttpRepository, IMemberRepository
{
    public HttpMemberRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public Task<Member?> FindByIdAsync(Guid memberId)
        => GetAsync<Member>($"api/members/{memberId}");

    public Task<Member?> FindByUserIdAsync(Guid userId)
        => GetAsync<Member>($"api/members/by-user/{userId}");

    public async Task<List<Member>> GetAllAsync(Guid? employerId = null)
        => await GetAsync<List<Member>>("api/members") ?? new List<Member>();

    public Task AddAsync(Member member)
        => PostAsync("api/members", member);

    public Task<bool> ExistsByMembershipNumberAsync(string membershipNumber, Guid? excludeMemberId = null) => throw new NotSupportedException();
    public Task<bool> ExistsByUserIdAsync(Guid userId) => throw new NotSupportedException();
}

