namespace PensionVault.Application.Interfaces;

public interface IUserService
{
    Task<string> UploadProfileImageAsync(Guid userId, string fileName, Stream fileStream, string extension);
    Task UpdateProfileAsync(Guid userId, string name, string? phone);
}
