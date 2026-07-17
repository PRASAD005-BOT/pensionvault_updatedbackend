using Members.Domain.Entities;

namespace Members.Services.DTOs;

public record CreateEmployerRequest(
    string CompanyName,
    string EmployerCode,
    string RegistrationNumber,
    string? Industry,
    RemittanceFrequency RemittanceFrequency,
    string? ContactDetails
);

public record UpdateEmployerRequest(
    string CompanyName,
    string? RegistrationNumber,
    string? Industry,
    RemittanceFrequency RemittanceFrequency,
    string? ContactDetails,
    EmployerStatus? Status
);


