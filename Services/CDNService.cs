using Microsoft.EntityFrameworkCore;
using OpentubeAPI.Data;
using OpentubeAPI.DTOs;
using OpentubeAPI.Models;

namespace OpentubeAPI.Services;

public class CDNService(OpentubeDBContext context) {
    private const string BasePath = "./Files";
    public static readonly string ImagePath = Path.Combine(BasePath, "Images");
    public static readonly string VideoPath = Path.Combine(BasePath, "Videos");
    public async Task<Result> GetImage(string filename, string? userId) {
        var file = await context.MediaFiles.FirstOrDefaultAsync(mf => mf.Filename == filename && mf.Visibility == FileVisibility.Public);
        var notFoundError = new Result(new Error("Filename", "Image not found")) {
            StatusCode = 404
        };
        if (file is null) {
            return notFoundError;
        }
        var filepath = Path.Combine(ImagePath, filename);
        return !File.Exists(filepath) ? notFoundError : new Result(await File.ReadAllBytesAsync(filepath));
    }
}