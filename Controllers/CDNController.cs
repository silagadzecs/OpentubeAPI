using Microsoft.AspNetCore.Mvc;
using OpentubeAPI.Services;
using OpentubeAPI.Utilities;

namespace OpentubeAPI.Controllers {
    [Route("cdn")]
    [ApiController]
    public class CDNController(CDNService cdnService) : ControllerBase {
        //TODO: Add userId detection if jwt is given
        [HttpGet("images/{filename}")]
        public async Task<IActionResult> GetPublicImage(string filename) {
            var result = await cdnService.GetImage(filename, null);
            var mimeType = ((byte[])result.Value!).GetMimeType();
            return !result.Success ? result.ToActionResult() : File((byte[])result.Value!, mimeType);
        }

        [HttpGet("videos/{videoId}/{filename}")]
        public async Task<IActionResult> GetVideo(string videoId, string filename) {
            var result = await cdnService.GetVideoFile(videoId, filename, null);
            var mimeType = ((byte[])result.Value!).GetMimeType();
            return !result.Success ? result.ToActionResult() : File((byte[])result.Value!, mimeType);
        }
    }
}
