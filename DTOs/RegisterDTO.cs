using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpentubeAPI.DTOs;

public class RegisterDTO {
    [FromForm, MinLength(1), MaxLength(30)]
    public required string DisplayName { get; set; }
    [FromForm, RegularExpression(@"^[\w]+$"), MinLength(2), MaxLength(100)]
    public required string Username { get; set; }
    [FromForm, RegularExpression(@"^[\w.+-]+@[a-zA-Z\d.-]+\.[a-zA-Z]{2,}$"), MaxLength(256)]
    public required string Email { get; set; }
    [FromForm, MinLength(4)]
    public required string Password { get; set; }
    public IFormFile? ProfilePicture { get; set; }
}