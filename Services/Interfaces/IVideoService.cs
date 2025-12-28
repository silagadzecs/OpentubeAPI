using OpentubeAPI.DTOs;

namespace OpentubeAPI.Services.Interfaces;

public interface IVideoService {
    Task<Result> Upload(IFormFile videoFile, IFormFile? thumbnail, VideoUploadDTO dto, string userId,
        CancellationToken cancellationToken);

    Task<Result> GetVideos(string? userid);
}