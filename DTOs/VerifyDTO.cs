namespace OpentubeAPI.DTOs;

public class VerifyDTO {
    public required string Email { get; set; }
    public required string Code { get; set; }
}