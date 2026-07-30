using System.IO;
using Microsoft.AspNetCore.Hosting;
using Members.Domain.Repositories;

namespace Members.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;

    public UserService(IUserRepository userRepo, IUnitOfWork unitOfWork, IWebHostEnvironment env)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _env = env;
    }

    public string? GetProfileImageUrl(Guid userId)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "profiles");
        if (!Directory.Exists(folder)) return null;

        var files = Directory.GetFiles(folder, $"{userId}.*");
        if (files.Length > 0)
        {
            return $"/uploads/profiles/{Path.GetFileName(files[0])}";
        }
        return null;
    }

    public async Task UpdateProfileAsync(Guid userId, string name, string? phone)
    {
        var user = await _userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        user.Name = name;
        if (phone != null)
            user.Phone = phone;
        await _unitOfWork.SaveChangesAsync();
    }
}
