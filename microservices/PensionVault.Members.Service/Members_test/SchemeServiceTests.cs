using Members.Domain.Entities;
using Members.Domain.Repositories;
using Members.Services;
using Members.Services.DTOs;
using Moq;
using NUnit.Framework;

namespace Members_test;

/// <summary>
/// Unit tests for <see cref="SchemeService"/> scheme validation and CRUD.
/// Repository is mocked; no HTTP clients.
/// </summary>
[TestFixture]
public class SchemeServiceTests
{
    private static (SchemeService svc, Mock<IFundSchemeRepository> repo, Mock<IUnitOfWork> uow)
        Build()
    {
        var repo = new Mock<IFundSchemeRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var svc = new SchemeService(repo.Object, uow.Object);
        return (svc, repo, uow);
    }

    [Test]
    public void CreateScheme_BlankName_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        var req = new CreateSchemeRequest("", SchemeType.EPF, 12m, 12m, 8m, 5, 100m, "Desc");
        Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(req));
    }

    [Test]
    public void CreateScheme_NoAlphabetic_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        // "123456" has no alphabetic characters
        var req = new CreateSchemeRequest("123456", SchemeType.EPF, 12m, 12m, 8m, 5, 100m, "Desc");
        Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(req));
    }

    [Test]
    public void CreateScheme_InterestRateZero_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        var req = new CreateSchemeRequest("EPF 2025", SchemeType.EPF, 12m, 12m, 0m, 5, 100m, "Desc");
        Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(req));
    }

    [Test]
    public async Task CreateScheme_Valid_SavesAndReturns()
    {
        var (svc, repo, uow) = Build();
        FundScheme? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<FundScheme>()))
            .Callback<FundScheme>(s => captured = s).Returns(Task.CompletedTask);

        var req = new CreateSchemeRequest("Employee Provident Fund", SchemeType.EPF, 12m, 12m, 8.15m, 5, 100m, "Standard EPF");
        var result = await svc.CreateAsync(req);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.SchemeName, Is.EqualTo("Employee Provident Fund"));
        Assert.That(captured.Status, Is.EqualTo(SchemeStatus.Active));
        Assert.That(captured.EmployeeContributionRate, Is.EqualTo(12m));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateScheme_NotFound_ThrowsKeyNotFound()
    {
        var (svc, repo, _) = Build();
        repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FundScheme?)null);

        var req = new UpdateSchemeRequest("EPF", 12m, 12m, 8m, 5, 100m, SchemeStatus.Active, "Desc");
        Assert.ThrowsAsync<KeyNotFoundException>(() => svc.UpdateAsync(Guid.NewGuid(), req));
    }

    [Test]
    public async Task UpdateScheme_Valid_UpdatesAndSaves()
    {
        var (svc, repo, uow) = Build();
        var scheme = new FundScheme
        {
            SchemeName = "EPF 2024",
            EmployeeContributionRate = 12m,
            Status = SchemeStatus.Active
        };
        repo.Setup(r => r.FindByIdAsync(scheme.SchemeId)).ReturnsAsync(scheme);

        var req = new UpdateSchemeRequest("EPF 2025", 13m, 13m, 8.5m, 5, 100m, SchemeStatus.Active, "Updated");
        var result = await svc.UpdateAsync(scheme.SchemeId, req);

        Assert.That(scheme.SchemeName, Is.EqualTo("EPF 2025"));
        Assert.That(scheme.EmployeeContributionRate, Is.EqualTo(13m));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
