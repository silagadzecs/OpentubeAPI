using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OpentubeAPI.Controllers
{
    [Route("cdn")]
    [ApiController]
    public class CDNController : ControllerBase {
        private const string BasePath = "./files";
        [HttpGet("files/{filename}")]
        public IActionResult GetPublicImage(string filename) {
            if (System.IO.File.Exists(Path.Combine(BasePath, filename))) return Ok();
            return NotFound();
            //TODO: Implement cdn and model for files in db
        }
    }
}
