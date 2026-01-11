using OpentubeAPI.DTOs;

namespace OpentubeAPI.Services.Interfaces;

public interface IVideoService {
    Task<Result> Upload(VideoUploadDTO dto, string userId,
        CancellationToken cancellationToken);

    Task<Result> GetVideos(string? userid);
    Task<Result> GetVideo(string videoId, string? userid);
    Task<Result> DeleteVideo(string videoId, string userId);
    Task<Result> EditVideo(string videoId, VideoEditDTO dto, string userId);
}