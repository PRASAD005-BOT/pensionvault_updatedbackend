using PensionVault.Application.DTOs.Annuity;

namespace PensionVault.Application.Interfaces;

public interface IAnnuityService
{
    Task<AnnuityResponse> CreateAnnuityAsync(CreateAnnuityRequest request);
    Task<AnnuityResponse> GetAnnuityAsync(Guid annuityId);
    Task<IEnumerable<PensionDisbursementResponse>> GetDisbursementsAsync(Guid annuityId);
    Task<PensionDisbursementResponse> ProcessDisbursementAsync(ProcessDisbursementRequest request);
    Task<IEnumerable<AnnuityResponse>> GetAllAnnuitiesAsync();
    Task<AnnuityResponse> ProcessNomineeSettlementAsync(Guid annuityId, NomineeSettlementRequest request);
    Task<AnnuityResponse> TerminateAnnuityAsync(Guid annuityId);
}
