using System.ComponentModel.DataAnnotations;

namespace OpentubeAPI.DTOs;

public class RegisterDTO {
    [MinLength(1), MaxLength(30)]
    public required string DisplayName { get; set; }
    [RegularExpression(@"^[\w]+$"), MinLength(2), MaxLength(100)]
    public required string Username { get; set; }
    [RegularExpression(@"^[\w.+-]+@[a-zA-Z\d.-]+\.[a-zA-Z]{2,}$"), MaxLength(256)]
    public required string Email { get; set; }
    [MinLength(4)]
    public required string Password { get; set; }
}