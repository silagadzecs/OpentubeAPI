using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace OpentubeAPI.Models;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Username), IsUnique = true)]
public class User {
    [Key] public Guid Id { get; init; } = Guid.NewGuid();
    [MaxLength(256)] public required string DisplayName { get; init; }
    [MaxLength(256)] public required string Username { get; init; }
    [MaxLength(256)] public required string Email { get; init; }
    [MaxLength(256)] public required string PasswordHash { get; init; }
    [MaxLength(256)] public string ProfilePicturePath { get; set; } = string.Empty;  
    [MaxLength(50)] public string? LastLoginIP { get; set; }
    public bool Verified { get; set; }
    public bool DeletionRequested { get; init; }
    public bool Active { get; init; } = true;
    public DateTimeOffset CreationDate { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLogin { get; set; }
    public UserRole Role { get; init; } = UserRole.User;
    
    public string GenerateAccessToken(JwtConfig config) {
        var key = new SymmetricSecurityKey(Convert.FromHexString(config.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jti = Guid.NewGuid().ToString();
        var token = new JwtSecurityToken(
            issuer: config.Issuer,
            audience: config.Audience,
            claims: [
                new Claim(JwtRegisteredClaimNames.Sub, Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                new Claim("dispname", DisplayName),
                new Claim("username", Username),
                new Claim("email", Email),
                new Claim("verified", Verified.ToString(), ClaimValueTypes.Boolean),
                new Claim("role", Role.ToString())
            ],
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(config.AccessHours),
            signingCredentials: creds
        );
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }
}
public enum UserRole : byte {
    User,
    Moderator,
    Admin,
    Owner,
}