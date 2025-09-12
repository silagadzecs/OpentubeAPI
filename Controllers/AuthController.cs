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
    public async Task<IActionResult> Register(IFormFile? profilePicture, [FromForm] RegisterDTO registrationData) {
        return (await authService.Register(registrationData, profilePicture)).ToActionResult();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO dto) {
        return (await authService.Login(dto, HttpContext.Connection.RemoteIpAddress?.ToString())).ToActionResult();
    }
}
