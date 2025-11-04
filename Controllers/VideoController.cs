using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpentubeAPI.DTOs;
using OpentubeAPI.Services;

namespace OpentubeAPI.Controllers {
    [ApiController]
    [Route("api/video")]
    public class VideoController(VideoService videoService) : ControllerBase {
        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier); 
        
        [Authorize]
        [HttpPost("upload")]
        [RequestSizeLimit(10_737_418_240)] //10 GiB
        [Produces("application/json")]
        public async Task<IActionResult> Upload(IFormFile videoFile, IFormFile? thumbnail, [FromForm] VideoUploadDTO dto) {
            return (await videoService.Upload(videoFile, thumbnail, dto, UserId!, HttpContext.RequestAborted)).ToActionResult();
        }
    }
}
