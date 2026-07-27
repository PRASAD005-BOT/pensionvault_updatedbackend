using System.Text.Json.Serialization;
using Members.Domain.Entities;

namespace Members.Services.DTOs;

public record CreateEmployerRequest(
    string CompanyName,
    string EmployerCode,
    string RegistrationNumber,
    string? Industry,
    RemittanceFrequency RemittanceFrequency,
    string? ContactEmail,
    string? ContactPhone,
    string? PortalJoinCode
);

public record UpdateEmployerRequest(
    string CompanyName,
    string? RegistrationNumber,
    string? Industry,
    RemittanceFrequency RemittanceFrequency,
    string? ContactEmail,
    string? ContactPhone,
    string? PortalJoinCode,

    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    EmployerStatus? Status = null
);