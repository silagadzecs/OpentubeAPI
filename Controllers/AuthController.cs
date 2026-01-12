using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpentubeAPI.DTOs;
using OpentubeAPI.Services;
using OpentubeAPI.Services.Interfaces;

namespace OpentubeAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase {
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetSelf() {
        return (await authService.GetSelf(UserId!)).ToActionResult();
    }

    [HttpPost("register")]
    [Produces("application/json")]
    public async Task<IActionResult> Register(RegisterDTO registrationData) {
        return (await authService.Register(registrationData)).ToActionResult();
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
        return (await authService.Login(dto, HttpContext)).ToActionResult();
    }

    [HttpPost("refresh")]
    [Produces("application/json")]
    public async Task<IActionResult> Refresh(string refreshToken, string userId) {
        return (await authService.RefreshTokens(refreshToken, userId)).ToActionResult();
    }

    [Authorize]
    [HttpGet("loggedin-devices")]
    public async Task<IActionResult> GetLoggedInDevices() {
        return (await authService.GetLoggedInDevices(UserId!)).ToActionResult();
    }

    [Authorize]
    [HttpPost("logout-devices")]
    public async Task<IActionResult> LogoutDevices(List<string> refreshTokenIds) {
        return (await authService.DeleteRefreshTokens(refreshTokenIds, UserId!)).ToActionResult();
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> EditUser(UserEditDTO dto) {
        return (await authService.EditUser(dto, UserId!)).ToActionResult();
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> DeleteUser([FromBody] string password) {
        return (await authService.DeleteUser(UserId!, password)).ToActionResult();
    }
    
}
