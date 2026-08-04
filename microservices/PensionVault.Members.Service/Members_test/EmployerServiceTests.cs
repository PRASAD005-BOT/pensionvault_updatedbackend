using Members.Domain.Entities;
using Members.Domain.Repositories;
using Members.Services;
using Members.Services.DTOs;
using Moq;
using NUnit.Framework;

namespace Members_test;

/// <summary>
/// Unit tests for <see cref="EmployerService"/> employer validation and CRUD.
/// Repositories are mocked; no HTTP clients.
/// </summary>
[TestFixture]
public class EmployerServiceTests
{
    private static (EmployerService svc, Mock<IEmployerRepository> repo, Mock<IUserRepository> userRepo, Mock<IUnitOfWork> uow)
        Build()
    {
        var repo = new Mock<IEmployerRepository>();
        var userRepo = new Mock<IUserRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var svc = new EmployerService(repo.Object, userRepo.Object, uow.Object);
        return (svc, repo, userRepo, uow);
    }

    [Test]
    public void CreateEmployer_BlankCode_ThrowsArgument()
    {
        var (svc, _, _, _) = Build();
        var req = new CreateEmployerRequest("Company", "", "REG123456", "Tech", RemittanceFrequency.Monthly, "test@test.com", "9999999999", "JOIN123");
        Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(req));
    }

    [Test]
    public void CreateEmployer_DuplicateCode_ThrowsInvalidOperation()
    {
        var (svc, repo, _, _) = Build();
        repo.Setup(r => r.ExistsByEmployerCodeAsync("ABC123")).ReturnsAsync(true);

        var req = new CreateEmployerRequest("Company", "ABC123", "REG123456", "Tech", RemittanceFrequency.Monthly, "test@test.com", "9999999999", "JOIN123");
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(req));
    }

    [Test]
    public void CreateEmployer_DuplicateRegNo_ThrowsInvalidOperation()
    {
        var (svc, repo, _, _) = Build();
        repo.Setup(r => r.ExistsByEmployerCodeAsync(It.IsAny<string>())).ReturnsAsync(false);
        repo.Setup(r => r.ExistsByRegistrationNumberAsync("REG123456")).ReturnsAsync(true);

        var req = new CreateEmployerRequest("Company", "ABC123", "REG123456", "Tech", RemittanceFrequency.Monthly, "test@test.com", "9999999999", "JOIN123");
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(req));
    }

    [Test]
    public async Task CreateEmployer_Valid_TrimsAndSavesAsUpperCase()
    {
        var (svc, repo, _, uow) = Build();
        repo.Setup(r => r.ExistsByEmployerCodeAsync(It.IsAny<string>())).ReturnsAsync(false);
        repo.Setup(r => r.ExistsByRegistrationNumberAsync(It.IsAny<string>())).ReturnsAsync(false);

        Employer? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Employer>()))
            .Callback<Employer>(e => captured = e).Returns(Task.CompletedTask);

        var req = new CreateEmployerRequest("Acme Corp", " abc123 ", " reg123456 ", "Tech", RemittanceFrequency.Monthly, "contact@acme.com", "9999999999", "CODE");
        var result = await svc.CreateAsync(req);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.EmployerCode, Is.EqualTo("ABC123"));
        Assert.That(captured.RegistrationNumber, Is.EqualTo("REG123456"));
        Assert.That(captured.Status, Is.EqualTo(EmployerStatus.Active));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateEmployer_NotFound_ThrowsKeyNotFound()
    {
        var (svc, repo, _, _) = Build();
        repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Employer?)null);

        var req = new UpdateEmployerRequest("Company Updated", "REG789", "Tech", RemittanceFrequency.Quarterly, "new@test.com", "8888888888", "CODE", EmployerStatus.Active);
        Assert.ThrowsAsync<KeyNotFoundException>(() => svc.UpdateAsync(Guid.NewGuid(), req));
    }

    [Test]
    public async Task GetByUserId_WithValidOrganisationId_ReturnsEmployer()
    {
        var (svc, repo, userRepo, _) = Build();
        var empId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, OrganisationId = empId };
        var employer = new Employer { EmployerId = empId, EmployerCode = "ACME", CompanyName = "Acme Corp" };

        userRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);
        repo.Setup(r => r.FindByIdAsync(empId)).ReturnsAsync(employer);

        var result = await svc.GetByUserIdAsync(userId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.EmployerId, Is.EqualTo(empId));
    }
}
