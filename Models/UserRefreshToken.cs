using System.ComponentModel.DataAnnotations;

namespace OpentubeAPI.Models;

public class UserRefreshToken {
    [MaxLength(128)]
    public required string RefreshToken { get; init; }
    public Guid UserId { get; init; }
    
    [MaxLength(128)]
    public string DeviceInfo { get; init; } = "Unknown";
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;
    //Nav Props
    public User User { get; init; } = null!;
}