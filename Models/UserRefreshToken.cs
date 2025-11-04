using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace OpentubeAPI.Models;

[Index(nameof(AccessJti), IsUnique = true)]
public class UserRefreshToken {
    [MaxLength(128)]
    public required string RefreshToken { get; init; }
    public Guid UserId { get; init; }
    
    [MaxLength(128)]
    public string DeviceInfo { get; init; } = "Unknown";
    
    [MaxLength(40)]
    public required string DeviceIp { get; init; }
    
    [MaxLength(36)]
    public required string AccessJti { get; init; }
    
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;
    //Nav Props
    public virtual User User { get; init; } = null!;
}