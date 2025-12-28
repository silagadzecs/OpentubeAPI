using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OpentubeAPI.Services;
using OpentubeAPI.Services.Interfaces;
using OpentubeAPI.Utilities;
using Serilog;

namespace OpentubeAPI.Controllers {
    [Route("cdn")]
    [ApiController]
    public class CDNController(ICDNService cdnService) : ControllerBase {
        private bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;
        private string? UserId => !IsAuthenticated ? null : User.FindFirstValue(ClaimTypes.NameIdentifier); 
        [HttpGet("images/{filename}")]
        public async Task<IActionResult> GetImage(string filename) {
            var result = await cdnService.GetImage(filename, UserId);
            var mimeType = ((byte[])result.Value!).GetMimeType();
            return !result.Success ? result.ToActionResult() : File((byte[])result.Value!, mimeType);
        }

        [HttpGet("videos/{videoId}/{filename}")]
        public async Task<IActionResult> GetVideo(string videoId, string filename) {
            var result = await cdnService.GetVideoFile(videoId, filename, UserId);
            var mimeType = ((byte[])result.Value!).GetMimeType();
            return !result.Success ? result.ToActionResult() : File((byte[])result.Value!, mimeType);
        }
    }
}
