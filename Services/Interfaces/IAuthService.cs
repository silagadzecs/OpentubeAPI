using OpentubeAPI.DTOs;
using OpentubeAPI.Models;

namespace OpentubeAPI.Services.Interfaces;

public interface IAuthService { 
    Task<Result> GetSelf(string userId);
    Task<Result> Register(RegisterDTO dto, IFormFile? profilePicture);
    Task<Result> Verify(string email, string code);
    Task<Result> ResendVerificationCode(string email);
    Task<Result> Login(LoginDTO dto, HttpContext context);
    Task<Result> RefreshTokens(string refreshToken, string userId);
    Task<Result> DeleteRefreshTokens(List<string> refreshTokenIds, string userId);
    Task<Result> GetLoggedInDevices(string userId);
    Task<bool> UserExistsAsync(string userId);
    Task<bool> IsAccessTokenValid(string jti);
    
}