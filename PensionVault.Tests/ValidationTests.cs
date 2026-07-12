using Moq;
using PensionVault.Application.DTOs.Annuity;
using PensionVault.Application.DTOs.Claims;
using PensionVault.Application.DTOs.Contributions;
using PensionVault.Application.DTOs.Ledger;
using PensionVault.Application.Services;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData(-100)]
    [InlineData(0)]
    public async Task ClaimService_SubmitClaim_ShouldThrow_WhenEligibleAmountIsNegativeOrZero(decimal amount)
    {
        // Arrange
        var claimRepo = new Mock<IClaimRepository>();
        var memberRepo = new Mock<IMemberRepository>();
        var accountRepo = new Mock<IFundAccountRepository>();
        var ledgerRepo = new Mock<ILedgerRepository>();
        var notificationRepo = new Mock<INotificationRepository>();
        var uow = new Mock<IUnitOfWork>();

        memberRepo.Setup(m => m.FindByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Member { MemberId = Guid.NewGuid(), UserId = Guid.NewGuid() });

        var userRepo = new Mock<IUserRepository>();

        var service = new ClaimService(
            claimRepo.Object,
            memberRepo.Object,
            accountRepo.Object,
            ledgerRepo.Object,
            notificationRepo.Object,
            userRepo.Object,
            uow.Object);

        var request = new CreateClaimRequest(Guid.NewGuid(), ClaimType.Retirement, amount);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitClaimAsync(request));
    }

    [Theory]
    [InlineData(-50)]
    [InlineData(0)]
    public async Task ClaimService_SubmitPartialWithdrawal_ShouldThrow_WhenRequestedAmountIsNegativeOrZero(decimal amount)
    {
        // Arrange
        var claimRepo = new Mock<IClaimRepository>();
        var memberRepo = new Mock<IMemberRepository>();
        var accountRepo = new Mock<IFundAccountRepository>();
        var ledgerRepo = new Mock<ILedgerRepository>();
        var notificationRepo = new Mock<INotificationRepository>();
        var uow = new Mock<IUnitOfWork>();

        memberRepo.Setup(m => m.FindByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Member { MemberId = Guid.NewGuid(), UserId = Guid.NewGuid() });

        var userRepo = new Mock<IUserRepository>();

        var service = new ClaimService(
            claimRepo.Object,
            memberRepo.Object,
            accountRepo.Object,
            ledgerRepo.Object,
            notificationRepo.Object,
            userRepo.Object,
            uow.Object);

        var request = new CreatePartialWithdrawalRequest(Guid.NewGuid(), amount, "Housing");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitPartialWithdrawalAsync(request));
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    public async Task ClaimService_DisburseClaim_ShouldThrow_WhenDisbursedAmountIsNegativeOrZero(decimal amount)
    {
        // Arrange
        var claimRepo = new Mock<IClaimRepository>();
        var memberRepo = new Mock<IMemberRepository>();
        var accountRepo = new Mock<IFundAccountRepository>();
        var ledgerRepo = new Mock<ILedgerRepository>();
        var notificationRepo = new Mock<INotificationRepository>();
        var uow = new Mock<IUnitOfWork>();

        var userRepo = new Mock<IUserRepository>();

        var service = new ClaimService(
            claimRepo.Object,
            memberRepo.Object,
            accountRepo.Object,
            ledgerRepo.Object,
            notificationRepo.Object,
            userRepo.Object,
            uow.Object);

        var request = new DisburseClaimRequest(amount, 0, "TestBank");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.DisburseClaimAsync(Guid.NewGuid(), request));
    }

    [Theory]
    [InlineData(-100, 100)]
    [InlineData(100, -100)]
    [InlineData(0, 100)]
    public async Task ContributionService_CreateRemittance_ShouldThrow_WhenSharesAreNegativeOrZero(decimal employeeShare, decimal employerShare)
    {
        // Arrange
        var contributionRepo = new Mock<IContributionRepository>();
        var employerRepo = new Mock<IEmployerRepository>();
        var memberRepo = new Mock<IMemberRepository>();
        var accountRepo = new Mock<IFundAccountRepository>();
        var ledgerRepo = new Mock<ILedgerRepository>();
        var notificationRepo = new Mock<INotificationRepository>();
        var userRepo = new Mock<IUserRepository>();
        var uow = new Mock<IUnitOfWork>();

        employerRepo.Setup(e => e.FindByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Employer { EmployerId = Guid.NewGuid() });

        var service = new ContributionService(
            contributionRepo.Object,
            employerRepo.Object,
            memberRepo.Object,
            accountRepo.Object,
            ledgerRepo.Object,
            notificationRepo.Object,
            userRepo.Object,
            uow.Object);

        var request = new CreateRemittanceRequest(
            Guid.NewGuid(),
            "2026-06",
            employeeShare,
            employerShare,
            0,
            1,
            new List<MemberContributionItem>
            {
                new MemberContributionItem(Guid.NewGuid(), employeeShare, employerShare, 0)
            });

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateRemittanceAsync(request));
    }

    [Theory]
    [InlineData(-100, 100)]
    [InlineData(100, -100)]
    [InlineData(0, 100)]
    public async Task AnnuityService_CreateAnnuity_ShouldThrow_WhenValueOrPensionIsNegativeOrZero(decimal purchaseValue, decimal monthlyPension)
    {
        // Arrange
        var annuityRepo = new Mock<IAnnuityRepository>();
        var requestRepo = new Mock<IAnnuityRequestRepository>();
        var memberRepo = new Mock<IMemberRepository>();
        var accountRepo = new Mock<IFundAccountRepository>();
        var ledgerRepo = new Mock<ILedgerRepository>();
        var notificationRepo = new Mock<INotificationRepository>();
        var contributionRepo = new Mock<IContributionRepository>();
        var uow = new Mock<IUnitOfWork>();

        memberRepo.Setup(m => m.FindByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Member { MemberId = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Rajesh" });

        var service = new AnnuityService(
            annuityRepo.Object,
            requestRepo.Object,
            accountRepo.Object,
            ledgerRepo.Object,
            memberRepo.Object,
            contributionRepo.Object,
            notificationRepo.Object,
            uow.Object);

        var request = new CreateAnnuityRequest(Guid.NewGuid(), AnnuityPlanType.LifeAnnuity, purchaseValue, monthlyPension, DateTime.UtcNow, "Bob");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAnnuityAsync(request));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task LedgerService_CreditInterest_ShouldThrow_WhenInterestRateIsNegativeOrZero(decimal interestRate)
    {
        // Arrange
        var accountRepo = new Mock<IFundAccountRepository>();
        var ledgerRepo = new Mock<ILedgerRepository>();
        var uow = new Mock<IUnitOfWork>();

        var service = new LedgerService(
            ledgerRepo.Object,
            accountRepo.Object,
            uow.Object);

        var request = new CreditInterestRequest(Guid.NewGuid(), "2025-2026", interestRate);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreditInterestAsync(request));
    }
}
