namespace Contributions.Domain.Entities;

public class FundScheme
{
    public Guid SchemeId { get; set; } = Guid.NewGuid();
    public string SchemeName { get; set; } = string.Empty;
    public SchemeType SchemeType { get; set; }
    public decimal EmployeeContributionRate { get; set; }
    public decimal EmployerContributionRate { get; set; }
    public decimal InterestRatePA { get; set; }
    public int VestingYears { get; set; }
    public decimal VestingPercent { get; set; }
    public SchemeStatus Status { get; set; } = SchemeStatus.Active;
}

