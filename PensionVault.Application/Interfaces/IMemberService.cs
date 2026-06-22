using PensionVault.Application.DTOs.Members;

namespace PensionVault.Application.Interfaces;

public interface IMemberService
{
    Task<IEnumerable<MemberResponse>> GetAllAsync(Guid? employerId = null);
    Task<MemberResponse> GetByIdAsync(Guid id);
    Task<MemberResponse> GetByUserIdAsync(Guid userId);
    Task<MemberResponse> CreateAsync(CreateMemberRequest request);
    Task<MemberResponse> UpdateAsync(Guid id, UpdateMemberRequest request);
    Task<IEnumerable<object>> GetFundAccountsAsync(Guid memberId);
    Task<IEnumerable<object>> GetContributionsAsync(Guid memberId);
    Task<IEnumerable<object>> GetLedgerAsync(Guid memberId);
    Task<IEnumerable<object>> GetClaimsAsync(Guid memberId);
    Task<MemberResponse> SelfEnrollAsync(Guid userId, SelfEnrollMemberRequest request);
    Task<MemberResponse> ApproveAsync(Guid id, ApproveMemberRequest request);
}
