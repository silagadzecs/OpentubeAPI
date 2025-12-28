using OpentubeAPI.Models;

namespace OpentubeAPI.DTOs;

public class VideoDTO(Video video) {
    public string Id { get; set; } = video.VideoFile.Filename;
    public string Title { get; set; } = video.Title;
    public string Description { get; set; } = video.Description;
    public string ThumbnailURL { get; set; } = $"/cdn/images/{video.ThumbnailFilename}";
    public string Visibility { get; set; } = video.VideoFile.Visibility.ToString();
    public UserDTO User { get; set; } = new(video.VideoFile.Owner);
}