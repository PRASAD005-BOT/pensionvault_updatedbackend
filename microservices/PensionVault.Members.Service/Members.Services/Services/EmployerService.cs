using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Members.Services.DTOs;
using Members.Domain.Entities;
using Members.Domain.Repositories;
using PensionVault.Shared.Contracts;
using System.Text.RegularExpressions;

namespace Members.Services;

public class EmployerService : IEmployerService
{
    private readonly IEmployerRepository _employerRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;

    public EmployerService(
        IEmployerRepository employerRepo,
        IUserRepository userRepo,
        IUnitOfWork unitOfWork)
    {
        _employerRepo = employerRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EmployerResponse>> GetAllAsync()
    {
        var employers = await _employerRepo.GetAllAsync();
        return employers.Select(ToResponse);
    }

    public async Task<EmployerResponse?> GetByIdAsync(Guid id)
    {
        var e = await _employerRepo.FindByIdAsync(id);
        return e is null ? null : ToResponse(e);
    }

    public async Task<EmployerResponse?> GetByUserIdAsync(Guid userId)
    {
        var user = await _userRepo.FindByIdAsync(userId);
        if (user?.OrganisationId is null)
            return null;

        var e = await _employerRepo.FindByIdAsync(user.OrganisationId.Value);
        return e is null ? null : ToResponse(e);
    }

    public async Task<EmployerResponse> CreateAsync(CreateEmployerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmployerCode))
            throw new ArgumentException("Employer ID is required.");

        var formattedCode = request.EmployerCode.Trim().ToUpperInvariant();
        var formattedRegNo = request.RegistrationNumber.Trim().ToUpperInvariant();

        // Validate alphanumeric rules (Must contain both letters and numbers)
        ValidateAlphanumericCode(formattedCode);
        ValidateAlphanumericRegNo(formattedRegNo);

        if (await _employerRepo.ExistsByEmployerCodeAsync(formattedCode))
            throw new InvalidOperationException("Employer ID already exists — choose a different one.");
        if (await _employerRepo.ExistsByRegistrationNumberAsync(formattedRegNo))
            throw new InvalidOperationException("Registration number already exists.");

        // Validate contact phone (if present)
        ValidateContactPhone(request.ContactPhone);

        var employer = new Employer
        {
            EmployerCode = formattedCode,
            CompanyName = request.CompanyName,
            RegistrationNumber = formattedRegNo,
            Industry = request.Industry,
            RemittanceFrequency = request.RemittanceFrequency,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            PortalJoinCode = request.PortalJoinCode,
            Status = EmployerStatus.Active
        };
        await _employerRepo.AddAsync(employer);
        await _unitOfWork.SaveChangesAsync();
        return ToResponse(employer);
    }

    public async Task<EmployerResponse> UpdateAsync(Guid id, UpdateEmployerRequest request)
    {
        var employer = await _employerRepo.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Employer not found.");

        string? formattedRegNo = null;
        if (!string.IsNullOrWhiteSpace(request.RegistrationNumber))
        {
            formattedRegNo = request.RegistrationNumber.Trim().ToUpperInvariant();
            ValidateAlphanumericRegNo(formattedRegNo);

            if (!string.Equals(employer.RegistrationNumber, formattedRegNo, StringComparison.OrdinalIgnoreCase)
                && await _employerRepo.ExistsByRegistrationNumberAsync(formattedRegNo))
                throw new InvalidOperationException("Registration number already exists.");
        }

        // Validate contact phone (if present)
        ValidateContactPhone(request.ContactPhone);

        employer.CompanyName = request.CompanyName;
        if (!string.IsNullOrWhiteSpace(formattedRegNo))
            employer.RegistrationNumber = formattedRegNo;

        employer.Industry = request.Industry;
        employer.RemittanceFrequency = request.RemittanceFrequency;
        employer.ContactEmail = request.ContactEmail;
        employer.ContactPhone = request.ContactPhone;
        employer.PortalJoinCode = request.PortalJoinCode;

        if (request.Status.HasValue)
        {
            employer.Status = request.Status.Value;
            bool isDeactivated = request.Status.Value == EmployerStatus.Deregistered ||
                                 request.Status.Value == EmployerStatus.Defaulter;

            await SyncLinkedUsersAsync(id, !isDeactivated);
        }

        await _unitOfWork.SaveChangesAsync();
        return ToResponse(employer);
    }

    public async Task<EmployerResponse> ApproveAsync(Guid id)
    {
        var employer = await _employerRepo.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Employer not found.");
        employer.Status = EmployerStatus.Active;

        await SyncLinkedUsersAsync(id, true);

        await _unitOfWork.SaveChangesAsync();
        return ToResponse(employer);
    }

    public async Task<EmployerResponse> RejectAsync(Guid id)
    {
        var employer = await _employerRepo.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Employer not found.");
        employer.Status = EmployerStatus.Deregistered;

        await SyncLinkedUsersAsync(id, false);

        await _unitOfWork.SaveChangesAsync();
        return ToResponse(employer);
    }

    private async Task SyncLinkedUsersAsync(Guid organisationId, bool isActive)
    {
        try
        {
            var method = _userRepo.GetType().GetMethod("GetByOrganisationIdAsync")
                      ?? _userRepo.GetType().GetMethod("FindByOrganisationIdAsync");

            if (method != null)
            {
                var task = (Task)method.Invoke(_userRepo, new object[] { organisationId })!;
                await task.ConfigureAwait(false);
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty?.GetValue(task) is IEnumerable<User> users)
                {
                    foreach (var u in users)
                    {
                        var prop = u.GetType().GetProperty("IsActive") ?? u.GetType().GetProperty("Active");
                        if (prop != null && prop.CanWrite)
                        {
                            prop.SetValue(u, isActive);
                        }
                    }
                }
            }
        }
        catch
        {
            // Silently skip if property mapping differs
        }
    }

    internal static string GenerateEmployerCode() => "EMP-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

    private static EmployerResponse ToResponse(Employer e) => new(
        e.EmployerId, e.EmployerCode, e.CompanyName, e.RegistrationNumber, e.Industry,
        e.EnrolledMemberCount, e.RemittanceFrequency.ToString(), e.ContactEmail, e.ContactPhone, e.PortalJoinCode, e.Status.ToString());

    private static void ValidateAlphanumericCode(string code)
    {
        bool hasLetter = Regex.IsMatch(code, "[A-Za-z]");
        bool hasDigit = Regex.IsMatch(code, "[0-9]");
        if (!hasLetter || !hasDigit)
        {
            throw new ArgumentException("Employer ID must contain both letters and numbers (e.g. EMP-1001).");
        }
    }

    private static void ValidateAlphanumericRegNo(string regNo)
    {
        bool hasLetter = Regex.IsMatch(regNo, "[A-Za-z]");
        bool hasDigit = Regex.IsMatch(regNo, "[0-9]");
        if (!hasLetter || !hasDigit)
        {
            throw new ArgumentException("Registration No must contain both letters and numbers (e.g. U74999MH...).");
        }
    }

    private static void ValidateContactPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return;
        var digits = Regex.Replace(phone, "\\D", "");
        if (digits.Length > 0 && digits.Length != 10)
            throw new ArgumentException("Contact phone must be a 10-digit number.");
    }
}