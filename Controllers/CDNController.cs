using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpentubeAPI.Services;
using OpentubeAPI.Utilities;

namespace OpentubeAPI.Controllers
{
    [Route("cdn")]
    [ApiController]
    public class CDNController(CDNService cdnService) : ControllerBase {
        private const string BasePath = "./files";
        [HttpGet("images/{filename}")]
        public async Task<IActionResult> GetPublicImage(string filename)
        {
            var result = await cdnService.GetImage(filename, null);
            var mimeType = ((byte[])result.Value!).GetMimeType();
            return !result.Success ? result.ToActionResult() : File((byte[])result.Value!, mimeType);
        }

        [HttpGet("videos/{filename}")]
        public async Task<IActionResult> GetVideo(string filename) {
            return Ok("Not implemented");
        }
    }
}
