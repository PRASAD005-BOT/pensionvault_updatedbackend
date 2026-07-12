using PensionVault.Application.DTOs.Annuity;

namespace PensionVault.Application.Interfaces;

public interface IAnnuityService
{
    // ── Approved Annuity Plans ────────────────────────────────────────────────
    Task<AnnuityResponse> CreateAnnuityAsync(CreateAnnuityRequest request);
    Task<AnnuityResponse> GetAnnuityAsync(Guid annuityId);
    Task<AnnuityResponse> UpdateAnnuityAsync(Guid annuityId, UpdateAnnuityRequest request);
    Task<IEnumerable<PensionDisbursementResponse>> GetDisbursementsAsync(Guid annuityId);
    Task<PensionDisbursementResponse> ProcessDisbursementAsync(ProcessDisbursementRequest request);
    Task<IEnumerable<AnnuityResponse>> GetAllAnnuitiesAsync();
    Task<AnnuityResponse> ProcessNomineeSettlementAsync(Guid annuityId, NomineeSettlementRequest request);
    Task<AnnuityResponse> TerminateAnnuityAsync(Guid annuityId);

    // ── Annuity Requests (DB-persisted, pre-approval) ─────────────────────────
    Task<AnnuityRequestResponse> SubmitAnnuityRequestAsync(SubmitAnnuityRequestDto dto);
    Task<IEnumerable<AnnuityRequestResponse>> GetPendingRequestsAsync();
    Task<IEnumerable<AnnuityRequestResponse>> GetMemberRequestsAsync(Guid memberId);
    Task<AnnuityRequestResponse> ApproveRequestAsync(Guid requestId, Guid reviewerUserId);
    Task<AnnuityRequestResponse> RejectRequestAsync(Guid requestId, Guid reviewerUserId, string? reviewNote);
    Task<AnnuityRequestResponse> CancelRequestAsync(Guid requestId, Guid memberId);

    // ── Eligibility ────────────────────────────────────────────────────────────
    Task<AnnuityEligibilityResponse> CheckEligibilityAsync(Guid memberId);
}

