using System.ComponentModel.DataAnnotations;

namespace OpentubeAPI.Models;

public class VerificationCode {
    [MaxLength(256)]
    public required string Email { get; init; }
    [MaxLength(6)]
    public required string Code { get; init; }
    public DateTimeOffset SentDate { get; init; } = DateTimeOffset.UtcNow;
}