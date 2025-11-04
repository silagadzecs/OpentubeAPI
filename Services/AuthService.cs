using System.Security.Cryptography;
using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using OpentubeAPI.Data;
using OpentubeAPI.DTOs;
using OpentubeAPI.Models;
using OpentubeAPI.Utilities;
using UAParser;

namespace OpentubeAPI.Services;

public class AuthService(OpentubeDBContext context, MailService mailService, JwtConfig jwtConfig) {
    public async Task<Result> GetSelf(string userId) {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId.ToGuid());
        return new Result(new UserDTO(user!));
    }
    public async Task<Result> Register(RegisterDTO dto, IFormFile? profilePicture) {
        dto.Email = dto.Email.Trim().ToLower();
        dto.Username = dto.Username.Trim().ToLower();
        var emailUser = await context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (emailUser is not null) {
            if (emailUser is { Verified: false, LastLogin: null } &&
                DateTimeOffset.UtcNow - emailUser.CreationDate >= TimeSpan.FromDays(30)) {
                context.Users.Remove(emailUser);
            } else {
                return new Result(new Error("Email", "Email already in use."));
            }
        }

        var usernameUser = await context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (usernameUser is not null) {
            if (usernameUser is { Verified: false, LastLogin: null } &&
                DateTimeOffset.UtcNow - usernameUser.CreationDate >= TimeSpan.FromDays(30)) {
                context.Users.Remove(usernameUser);
            } else {
                return new Result(new Error("Username", "Username already taken."));
            }
        }

        var user = new User {
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            Username = dto.Username,
            PasswordHash = Argon2.Hash(dto.Password)
        };
        
        if (profilePicture is not null) {
            Directory.CreateDirectory(CDNService.ImagePath);
            var filename = Guid.NewGuid().ToString();
            var extension = profilePicture.FileName.Split(".").Last();
            filename = $"{filename}.{extension}";
            var fileDir = Path.Combine(CDNService.ImagePath, filename);
            try {
                await using var stream = new FileStream(
                    fileDir,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite
                );
                await profilePicture.CopyToAsync(stream);

                if (!stream.GetMimeType().StartsWith("image/")) {
                    File.Delete(fileDir);
                    return new Result(new Error("profilePicture", "Invalid profile picture file"));
                }
            } catch {
                File.Delete(fileDir);
                throw;
            }
            
            user.ProfilePicture = filename;
            await context.MediaFiles.AddAsync(new MediaFile {
                Filename = filename,
                FileType = FileType.Image,
                OwnerId = user.Id,
                Visibility = Visibility.Public
            });
        }
        var code = GenerateCode();
        await context.VerificationCodes.AddAsync(
            new VerificationCode {
                Email = user.Email,
                Code = code,
            }
        );
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        _ = mailService.SendCode(code, user.Email);
        return new Result("Successfully registered, check your email for a verification code.");
    }

    public async Task<Result> Verify(string email, string code) { 
        email = email.Trim().ToLower();
        code = code.Trim();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) 
            return new Result(new Error("Email", "This email is not registered"));
        var dbCode = await context.VerificationCodes.FirstOrDefaultAsync(vc => vc.Email == email && vc.Code == code);
        var isExpired = dbCode is not null && (DateTimeOffset.UtcNow - dbCode.SentDate) >= TimeSpan.FromHours(1);
        if (dbCode is null || isExpired)
            return new Result(new Error("Code", "Invalid Code"));
        context.VerificationCodes.Remove(dbCode);
        user.Verified = true;
        await context.SaveChangesAsync();
        return new Result("Successfully verified");
    }

    public async Task<Result> ResendVerificationCode(string email) {
        email = email.Trim().ToLower();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
            return new Result(new Error("Email", "This email is not registered"));
        if (user.Verified)
            return new Result(new Error("Email", "This email is already verified"));
        context.VerificationCodes.RemoveRange(context.VerificationCodes.Where(vc => vc.Email == user.Email));
        var code = GenerateCode();
        context.VerificationCodes.Add(
            new VerificationCode {
                Email = user.Email,
                Code = code,
            }
        );
        await context.SaveChangesAsync();
        _ = mailService.SendCode(code, user.Email);
        return new Result("Sent a new verification code to your email.");
    }

    public async Task<Result> Login(LoginDTO dto, HttpContext httpContext) {
        dto.Username = dto.Username.Trim().ToLower();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username || u.Email == dto.Username);
        var credsError = new Result(new Error("Credentials", "Incorrect username or password"));
        if (user is null) return credsError;
        if (!user.Verified) return new Result(new Error("Email", "Email not verified."));
        if (!Argon2.Verify(user.PasswordHash, dto.Password)) return credsError;
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        user.LastLogin = DateTimeOffset.UtcNow;
        user.LastLoginIP = ip;
        var uaInfo = Parser.GetDefault().Parse(httpContext.Request.Headers.UserAgent.ToString());
        var deviceInfo = $"{uaInfo.UA.Family} on {uaInfo.OS.Family} (Device: {uaInfo.Device.Family})";
        var (accessToken, jti) = user.GenerateAccessToken(jwtConfig);
        var refreshToken = AddRefreshToken(
            user.Id,
            deviceInfo,
            ip ?? "Unknown",
            jti);
        await context.SaveChangesAsync();
        return new Result(new {
            accessToken,
            refreshToken
        });
    }

    public async Task<Result> RefreshTokens(string refreshToken, string userId) {
        var hashedToken = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(refreshToken)));
        var token = await context.UserRefreshTokens
            .Include(urt => urt.User)
            .FirstOrDefaultAsync(urt => urt.UserId == userId.ToGuid() 
                                        && urt.RefreshToken == hashedToken 
                                        && DateTimeOffset.UtcNow - urt.Created < TimeSpan.FromHours(jwtConfig.RefreshHours));
        if (token is null) return new Result(new Error("Token", "Invalid refresh token"));
        var (newToken, jti) = token.User.GenerateAccessToken(jwtConfig);
        var newRefreshToken = AddRefreshToken( //Adds a refresh token to the context (without saving changes)
            token.User.Id,
            token.DeviceInfo,
            token.DeviceIp,
            jti);
        context.UserRefreshTokens.Remove(token);
        await context.SaveChangesAsync();
        return new Result(new {
            accessToken = newToken,
            refreshToken = newRefreshToken
        });
    }

    public async Task<bool> UserExistsAsync(string userId) {
        return (await context.Users.FindAsync(userId.ToGuid())) is not null;
    }

    public async Task<bool> AccessTokenValid(string jti) {
        var refreshToken = await context.UserRefreshTokens.FirstOrDefaultAsync(urt => 
            urt.AccessJti == jti);
        if (refreshToken is null) return false;
        return DateTimeOffset.UtcNow - refreshToken.Created < TimeSpan.FromHours(jwtConfig.AccessHours);
    }

    private string AddRefreshToken(Guid userId, string deviceInfo, string ip, string jti) {
        var refreshToken = RandomNumberGenerator.GetHexString(64, true);
        var refreshTokenEntry = new UserRefreshToken {
            RefreshToken = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(refreshToken))),
            UserId = userId,
            DeviceInfo = deviceInfo,
            DeviceIp = ip,
            AccessJti = jti
        };
        context.UserRefreshTokens.Add(refreshTokenEntry);
        return refreshToken;
    }

    private static string GenerateCode() {
        var code = RandomNumberGenerator.GetInt32(0, 1000000);
        return code.ToString("D6");
    }
}