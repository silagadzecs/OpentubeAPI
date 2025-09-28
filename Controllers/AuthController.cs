using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpentubeAPI.DTOs;
using OpentubeAPI.Services;

namespace OpentubeAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase {
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetSelf() {
        return Ok("Not implemented.");
    }

    [HttpPost("register")]
    [Produces("application/json")]
    public async Task<IActionResult> Register(IFormFile? profilePicture, [FromForm] RegisterDTO registrationData) {
        return (await authService.Register(registrationData, profilePicture)).ToActionResult();
    }

    [HttpPost("verify")]
    [Produces("application/json")]
    public async Task<IActionResult> Verify(VerifyDTO dto) {
        return (await authService.Verify(dto.Email, dto.Code)).ToActionResult();
    }

    [HttpPost("resend-code")]
    [Produces("application/json")]
    public async Task<IActionResult> ResendCode(string email) {
        return (await authService.ResendVerificationCode(email)).ToActionResult();
    }

    [HttpPost("login")]
    [Produces("application/json")]
    public async Task<IActionResult> Login(LoginDTO dto) {
        return (await authService.Login(dto, HttpContext.Connection.RemoteIpAddress?.ToString())).ToActionResult();
    }
}
