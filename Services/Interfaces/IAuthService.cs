using OpentubeAPI.DTOs;
using OpentubeAPI.Models;

namespace OpentubeAPI.Services.Interfaces;

public interface IAuthService { 
    Task<Result> GetSelf(string userId);
    Task<Result> Register(RegisterDTO dto);
    Task<Result> Verify(string email, string code);
    Task<Result> ResendVerificationCode(string email);
    Task<Result> Login(LoginDTO dto, HttpContext context);
    Task<Result> RefreshTokens(string refreshToken, string userId);
    Task<Result> DeleteRefreshTokens(List<string> refreshTokenIds, string userId);
    Task<Result> GetLoggedInDevices(string userId);
    Task<Result> EditUser(UserEditDTO dto, string userId);
    Task<Result> DeleteUser(string userId, string currentPassword);
    Task<bool> UserExistsAsync(string userId);
    Task<bool> IsAccessTokenValid(string jti);
    
}