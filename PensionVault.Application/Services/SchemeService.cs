using PensionVault.Application.DTOs.Schemes;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Application.Services;

public class SchemeService : ISchemeService
{
    private readonly IFundSchemeRepository _schemeRepo;
    private readonly IUnitOfWork _unitOfWork;

    public SchemeService(IFundSchemeRepository schemeRepo, IUnitOfWork unitOfWork)
    {
        _schemeRepo = schemeRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SchemeResponse>> GetAllAsync()
    {
        var schemes = await _schemeRepo.GetAllAsync();
        return schemes.Select(ToResponse);
    }

    public async Task<SchemeResponse> GetByIdAsync(Guid id)
    {
        var s = await _schemeRepo.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Scheme not found.");
        return ToResponse(s);
    }

    public async Task<SchemeResponse> CreateAsync(CreateSchemeRequest request)
    {
        var scheme = new FundScheme
        {
            SchemeName = request.SchemeName,
            SchemeType = request.SchemeType,
            EmployeeContributionRate = request.EmployeeContributionRate,
            EmployerContributionRate = request.EmployerContributionRate,
            InterestRatePA = request.InterestRatePA,
            VestingSchedule = request.VestingSchedule,
            Status = SchemeStatus.Active
        };
        await _schemeRepo.AddAsync(scheme);
        await _unitOfWork.SaveChangesAsync();
        return ToResponse(scheme);
    }

    public async Task<SchemeResponse> UpdateAsync(Guid id, UpdateSchemeRequest request)
    {
        var scheme = await _schemeRepo.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Scheme not found.");
        scheme.SchemeName = request.SchemeName;
        scheme.EmployeeContributionRate = request.EmployeeContributionRate;
        scheme.EmployerContributionRate = request.EmployerContributionRate;
        scheme.InterestRatePA = request.InterestRatePA;
        scheme.VestingSchedule = request.VestingSchedule;
        scheme.Status = request.Status;
        await _unitOfWork.SaveChangesAsync();
        return ToResponse(scheme);
    }

    private static SchemeResponse ToResponse(FundScheme s) => new(
        s.SchemeId, s.SchemeName, s.SchemeType,
        s.EmployeeContributionRate, s.EmployerContributionRate,
        s.InterestRatePA, s.VestingSchedule, s.Status);
}
