namespace OpentubeAPI.Models;

public class MediaFile {
    public required string Filename { get; set; }
    public required FileType FileType { get; set; }
    public required Guid OwnerId { get; set; }
    public required FileVisibility Visibility { get; set; }
}

public enum FileVisibility : byte {
    Public,
    Private
}

public enum FileType : byte {
    Image,
    Video
}