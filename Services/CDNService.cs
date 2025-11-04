using Microsoft.EntityFrameworkCore;
using OpentubeAPI.Data;
using OpentubeAPI.DTOs;
using OpentubeAPI.Models;
using OpentubeAPI.Utilities;

namespace OpentubeAPI.Services;

public class CDNService(OpentubeDBContext context) {
    private const string BasePath = "./Files";
    public static readonly string ImagePath = Path.Combine(BasePath, "Images");
    public static readonly string VideoPath = Path.Combine(BasePath, "Videos");
    
    private static readonly Result NotFoundError = new(new Error("Filename", "File not found")) {
        StatusCode = 404
    };
    private static readonly Result ForbiddenError = new(new Error("Forbidden", "You are not allowed to view this file")) {
        StatusCode = 403
    };
    public async Task<Result> GetImage(string filename, string? userId) {
        var image = await context.MediaFiles.FirstOrDefaultAsync(mf => mf.Filename == filename);
        if (image is null) {
            return NotFoundError;
        }

        if (image.Visibility is Visibility.Private && (userId ?? string.Empty).ToGuid() != image.OwnerId) {
            return ForbiddenError;
        }  
        var filepath = Path.Combine(ImagePath, filename);
        return !File.Exists(filepath) ? NotFoundError : new Result(await File.ReadAllBytesAsync(filepath));
    }

    public async Task<Result> GetVideoFile(string videoId, string filename, string? userId) {
        var video = await context.MediaFiles.FirstOrDefaultAsync(mf => mf.Filename == filename);
        if (video is null) {
            return NotFoundError;
        }

        if (video.Visibility is Visibility.Private && (userId ?? string.Empty).ToGuid() != video.OwnerId) {
            return ForbiddenError;
        }

        var filepath = Path.Combine(VideoPath, videoId, filename);
        return !File.Exists(filepath) ? NotFoundError : new Result(await File.ReadAllBytesAsync(filepath));
    }
}