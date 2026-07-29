using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Members.Services.DTOs;
using Members.Domain.Entities;
using Members.Domain.Repositories;
using PensionVault.Shared.Contracts;

namespace Members.Services;

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
        ValidateSchemeRequest(
            request.SchemeName,
            request.EmployeeContributionRate,
            request.EmployerContributionRate,
            request.InterestRatePA,
            request.Description);

        var scheme = new FundScheme
        {
            SchemeName = request.SchemeName.Trim(),
            SchemeType = request.SchemeType,
            EmployeeContributionRate = request.EmployeeContributionRate,
            EmployerContributionRate = request.EmployerContributionRate,
            InterestRatePA = request.InterestRatePA,
            VestingYears = request.VestingYears,
            VestingPercent = request.VestingPercent,
            Description = request.Description.Trim(),
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

        ValidateSchemeRequest(
            request.SchemeName,
            request.EmployeeContributionRate,
            request.EmployerContributionRate,
            request.InterestRatePA,
            request.Description);

        scheme.SchemeName = request.SchemeName.Trim();
        scheme.EmployeeContributionRate = request.EmployeeContributionRate;
        scheme.EmployerContributionRate = request.EmployerContributionRate;
        scheme.InterestRatePA = request.InterestRatePA;
        scheme.VestingYears = request.VestingYears;
        scheme.VestingPercent = request.VestingPercent;
        scheme.Description = request.Description.Trim();
        scheme.Status = request.Status;
        await _unitOfWork.SaveChangesAsync();
        return ToResponse(scheme);
    }

    private static void ValidateSchemeRequest(
        string schemeName,
        decimal employeeRate,
        decimal employerRate,
        decimal interestRate,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(schemeName))
            throw new ArgumentException("Scheme Name is required.");

        // Must contain at least one alphabetic letter
        if (!Regex.IsMatch(schemeName, @"[a-zA-Z]"))
            throw new ArgumentException("Scheme Name must contain alphabetic characters.");

        if (employeeRate < 0)
            throw new ArgumentException("Employee Contribution Rate cannot be negative.");

        if (employerRate < 0)
            throw new ArgumentException("Employer Contribution Rate cannot be negative.");

        if (employeeRate == 0 && employerRate == 0)
            throw new ArgumentException("At least one contribution rate (Employee or Employer) must be greater than 0%.");

        if (interestRate <= 0 || interestRate > 100)
            throw new ArgumentException("Interest Rate Per Annum must be greater than 0% and cannot exceed 100%.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.");

        // Description must contain at least one alphabetic character
        if (!Regex.IsMatch(description, @"[a-zA-Z]"))
            throw new ArgumentException("Description must contain alphabetic characters (cannot be numbers only).");
    }

    private static SchemeResponse ToResponse(FundScheme s) => new(
        s.SchemeId, s.SchemeName, s.SchemeType.ToString(),
        s.EmployeeContributionRate, s.EmployerContributionRate,
        s.InterestRatePA, s.VestingYears, s.VestingPercent, s.Status.ToString(), s.Description);
}