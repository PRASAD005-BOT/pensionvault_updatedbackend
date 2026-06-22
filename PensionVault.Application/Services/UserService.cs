using PensionVault.Application.Interfaces;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository userRepo, IUnitOfWork unitOfWork)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> UploadProfileImageAsync(Guid userId, string fileName, Stream fileStream, string extension)
    {
        var user = await _userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        // Build the file URL — actual file-system write is handled by the controller which has IWebHostEnvironment
        var fileUrl = $"/uploads/profiles/{fileName}";
        user.ProfileImageUrl = fileUrl;
        await _unitOfWork.SaveChangesAsync();
        return fileUrl;
    }

    public async Task UpdateProfileAsync(Guid userId, string name, string? phone)
    {
        var user = await _userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        user.Name = name;
        user.Phone = phone;
        await _unitOfWork.SaveChangesAsync();
    }
}
