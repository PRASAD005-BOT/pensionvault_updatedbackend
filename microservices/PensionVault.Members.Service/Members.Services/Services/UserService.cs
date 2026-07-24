using Members.Domain.Repositories;

namespace Members.Services;

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

        var fileUrl = $"/uploads/profiles/{fileName}";
        user.ProfileImageUrl = fileUrl;
        await _unitOfWork.SaveChangesAsync();
        return fileUrl;
    }

    public async Task UpdateProfileAsync(Guid userId, string name, string? phone)
    {
        var user = await _userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (!string.IsNullOrWhiteSpace(name))
            user.Name = name.Trim();

        // Phone is a non-nullable column; only overwrite it when a value is supplied
        // so callers that omit the phone (e.g. the profile name edit) don't null it out.
        if (phone != null)
            user.Phone = phone;

        await _unitOfWork.SaveChangesAsync();
    }
}


