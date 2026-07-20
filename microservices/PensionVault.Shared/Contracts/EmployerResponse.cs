namespace PensionVault.Shared.Contracts;

public record EmployerResponse(
    Guid EmployerId,
    string EmployerCode,
    string CompanyName,
    string RegistrationNumber,
    string? Industry,
    int EnrolledMemberCount,
    string RemittanceFrequency,
    string? ContactDetails,
    string Status
);


