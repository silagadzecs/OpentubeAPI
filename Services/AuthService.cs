using System.Security.Cryptography;
using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using OpentubeAPI.Data;
using OpentubeAPI.DTOs;
using OpentubeAPI.Models;

namespace OpentubeAPI.Services;

public class AuthService(OpentubeDBContext context, MailService mailService, JwtConfig jwtConfig) {
    
    
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
            if (!profilePicture.ContentType.StartsWith("image/")) {
                return new Result(new Error("profilePicture", "Invalid profile picture file"));
            }
            const string dir = "./files/images/public";
            Directory.CreateDirectory(dir);
            var filename = Guid.NewGuid().ToString();
            var extension = profilePicture.FileName.Split(".").Last();
            var fileDir = Path.Combine(dir, $"{filename}.{extension}");
            await using var stream = new FileStream(
                fileDir,
                FileMode.CreateNew,
                FileAccess.Write
            );
            await profilePicture.CopyToAsync(stream);
            user.ProfilePicturePath = fileDir;
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

    public async Task<Result> Login(LoginDTO dto, string? ipAddress) {
        dto.Username = dto.Username.Trim().ToLower();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username || u.Email == dto.Username);
        var credsError = new Result(new Error("Credentials", "Incorrect username or password"));
        if (user is null) return credsError;
        // if (!user.Verified) return new Result(new Error("Email", "Email not verified."));
        if (!Argon2.Verify(user.PasswordHash, dto.Password)) return credsError;
        user.LastLogin = DateTimeOffset.UtcNow;
        user.LastLoginIP = ipAddress;
        await context.SaveChangesAsync();
        return new Result(new { accessToken = user.GenerateAccessToken(jwtConfig) });
    }

    private static string GenerateCode() {
        var code = RandomNumberGenerator.GetInt32(0, 1000000);
        return code.ToString("D6");
    }
}