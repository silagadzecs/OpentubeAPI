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
        
        [HttpGet]
        public async Task<IActionResult> GetVideos() {
            return (await videoService.GetVideos(UserId)).ToActionResult();
        }
        
        [HttpGet("{videoId}")]
        public async Task<IActionResult> GetVideo(string videoId) {
            return (await videoService.GetVideo(videoId, UserId)).ToActionResult();
        }
        
        [Authorize]
        [HttpPost("upload")]
        [RequestSizeLimit(10_737_418_240)] //10 GiB
        [Produces("application/json")]
        public async Task<IActionResult> Upload(VideoUploadDTO dto) {
            return (await videoService.Upload(dto, UserId!, HttpContext.RequestAborted)).ToActionResult();
        }
        
        [Authorize]
        [HttpPut("{videoId}")]
        public async Task<IActionResult> EditVideo(string videoId, VideoEditDTO dto) {
            return (await videoService.EditVideo(videoId, dto, UserId!)).ToActionResult();
        }
        
        [Authorize]
        [HttpDelete("{videoId}")]
        public async Task<IActionResult> DeleteVideo(string videoId) {
            return (await videoService.DeleteVideo(videoId, UserId!)).ToActionResult();
        }

    }
}
