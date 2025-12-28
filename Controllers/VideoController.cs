using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpentubeAPI.DTOs;
using OpentubeAPI.Services.Interfaces;

namespace OpentubeAPI.Controllers {
    [ApiController]
    [Route("api/videos")]
    public class VideoController(IVideoService videoService) : ControllerBase {
        private bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;
        private string? UserId => !IsAuthenticated ? null : User.FindFirstValue(ClaimTypes.NameIdentifier); 
        
        [Authorize]
        [HttpPost("upload")]
        [RequestSizeLimit(10_737_418_240)] //10 GiB
        [Produces("application/json")]
        public async Task<IActionResult> Upload(IFormFile videoFile, IFormFile? thumbnail, [FromForm] VideoUploadDTO dto) {
            return (await videoService.Upload(videoFile, thumbnail, dto, UserId!, HttpContext.RequestAborted)).ToActionResult();
        }

        [HttpGet]
        public async Task<IActionResult> GetVideos() {
            return (await videoService.GetVideos(UserId)).ToActionResult();
        }
    }
}
