using Members.Services.DTOs;
using Members.Domain.Entities;
using Members.Domain.Repositories;
using PensionVault.Shared.Contracts;

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

    public async Task<EmployerResponse> GetByIdAsync(Guid id)
    {
        var e = await _employerRepo.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Employer not found.");
        return ToResponse(e);
    }

    public async Task<EmployerResponse> GetByUserIdAsync(Guid userId)
    {
        var user = await _userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.OrganisationId == null)
            throw new KeyNotFoundException("User is not associated with an organisation.");

        var e = await _employerRepo.FindByIdAsync(user.OrganisationId.Value)
            ?? throw new KeyNotFoundException("No employer profile found.");
        return ToResponse(e);
    }

    public async Task<EmployerResponse> CreateAsync(CreateEmployerRequest request)
    {
        if (await _employerRepo.ExistsByRegistrationNumberAsync(request.RegistrationNumber))
            throw new InvalidOperationException("Registration number already exists.");

        var employer = new Employer
        {
            CompanyName = request.CompanyName,
            RegistrationNumber = request.RegistrationNumber,
            Industry = request.Industry,
            RemittanceFrequency = request.RemittanceFrequency,
            ContactDetails = request.ContactDetails,
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
        employer.CompanyName = request.CompanyName;
        employer.Industry = request.Industry;
        employer.RemittanceFrequency = request.RemittanceFrequency;
        employer.ContactDetails = request.ContactDetails;
        employer.Status = request.Status;
        await _unitOfWork.SaveChangesAsync();
        return ToResponse(employer);
    }

    private static EmployerResponse ToResponse(Employer e) => new(
        e.EmployerId, e.CompanyName, e.RegistrationNumber, e.Industry,
        e.EnrolledMemberCount, e.RemittanceFrequency.ToString(), e.ContactDetails, e.Status.ToString());
}




