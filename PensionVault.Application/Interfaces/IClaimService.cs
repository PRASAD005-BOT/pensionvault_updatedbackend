using PensionVault.Application.DTOs.Claims;

namespace PensionVault.Application.Interfaces;

public interface IClaimService
{
    Task<ClaimResponse> SubmitClaimAsync(CreateClaimRequest request);
    Task<IEnumerable<ClaimResponse>> GetAllClaimsAsync();
    Task<ClaimResponse> GetClaimAsync(Guid claimId);
    Task<ClaimResponse> ReviewClaimAsync(Guid claimId, Guid processedById);
    Task<ClaimResponse> ApproveClaimAsync(Guid claimId, Guid processedById);
    Task<ClaimResponse> RejectClaimAsync(Guid claimId, Guid processedById);
    Task<DisbursementResponse> DisburseClaimAsync(Guid claimId, DisburseClaimRequest request);
    Task<ClaimResponse> SubmitPartialWithdrawalAsync(CreatePartialWithdrawalRequest request);
    Task<DisbursementResponse> DisbursePartialWithdrawalAsync(Guid claimId, DisbursePartialWithdrawalRequest request);
}
