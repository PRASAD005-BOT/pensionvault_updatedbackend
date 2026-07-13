using Members.Domain.Repositories;
namespace Members.Services;

public interface IUserService
{
    Task<string> UploadProfileImageAsync(Guid userId, string fileName, Stream fileStream, string extension);
    Task UpdateProfileAsync(Guid userId, string name, string? phone);
}



