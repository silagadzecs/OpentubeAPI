using OpentubeAPI.DTOs;

namespace OpentubeAPI.Services.Interfaces;

public interface ICDNService {
    Task<Result> GetImage(string filename, string? userId);
    Task<Result> GetVideoFile(string videoId, string filename, string? userId);
}