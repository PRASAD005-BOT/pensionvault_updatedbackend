using Members.Domain.Repositories;
namespace Members.Services;

public interface IUserService
{
    string? GetProfileImageUrl(Guid userId);
    Task UpdateProfileAsync(Guid userId, string name, string? phone);
}



