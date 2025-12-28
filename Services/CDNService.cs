using Microsoft.EntityFrameworkCore;
using OpentubeAPI.Data;
using OpentubeAPI.DTOs;
using OpentubeAPI.Models;
using OpentubeAPI.Services.Interfaces;
using OpentubeAPI.Utilities;

namespace OpentubeAPI.Services;

public class CDNService(OpentubeDBContext context) : ICDNService {
    private static string? _imagePath;
    private static string? _videoPath;

    public static string ImagePath {
        get => _imagePath ?? throw new Exception("Tried to get CDN image path before initialization");
        private set => _imagePath = value;
    }

    public static string VideoPath {
        get => _videoPath ?? throw new Exception("Tried to get CDN video path before initialization");
        private set => _videoPath = value;
    }

    public static void SetPaths(string basePath) {
        ImagePath =  Path.Combine(basePath, "Images");
        VideoPath =  Path.Combine(basePath, "Videos");
        Directory.CreateDirectory(ImagePath);
        Directory.CreateDirectory(VideoPath);
    }
    
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
        var video = await context.MediaFiles.FirstOrDefaultAsync(mf => mf.Filename == videoId);
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