using Microsoft.AspNetCore.Http;
using PensionVault.Application.DTOs.Members;
using PensionVault.Application.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Claims.Service.ProxyServices;

public class HttpMemberService : BaseHttpRepository, IMemberService
{
    public HttpMemberService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public Task<MemberResponse> GetByUserIdAsync(Guid userId)
        => GetAsync<MemberResponse>($"api/members/by-user/{userId}")!;

    public Task<IEnumerable<MemberResponse>> GetAllAsync(Guid? employerId = null) => throw new NotSupportedException();
    public Task<MemberResponse> GetByIdAsync(Guid id) => throw new NotSupportedException();
    public Task<MemberResponse> CreateAsync(CreateMemberRequest request) => throw new NotSupportedException();
    public Task<MemberResponse> UpdateAsync(Guid id, UpdateMemberRequest request) => throw new NotSupportedException();
    public Task<MemberResponse> SelfEnrollAsync(Guid userId, SelfEnrollMemberRequest request) => throw new NotSupportedException();
    public Task<MemberResponse> ApproveAsync(Guid id, ApproveMemberRequest request) => throw new NotSupportedException();
    public Task<IEnumerable<object>> GetFundAccountsAsync(Guid memberId) => throw new NotSupportedException();
    public Task<IEnumerable<object>> GetContributionsAsync(Guid memberId) => throw new NotSupportedException();
    public Task<IEnumerable<object>> GetLedgerAsync(Guid memberId) => throw new NotSupportedException();
    public Task<IEnumerable<object>> GetClaimsAsync(Guid memberId) => throw new NotSupportedException();
}

