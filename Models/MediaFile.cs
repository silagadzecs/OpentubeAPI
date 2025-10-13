using System.ComponentModel.DataAnnotations;

namespace OpentubeAPI.Models;

public class MediaFile {
    public Guid Id { get; init; } = Guid.NewGuid();
    [MaxLength(256)]
    public required string Filename { get; init; }
    public required FileType FileType { get; init; }
    public required Guid OwnerId { get; init; }
    public required FileVisibility Visibility { get; init; }
    
    public User Owner { get; init; } = null!;
}

public enum FileVisibility : byte {
    Public,
    Private
}

public enum FileType : byte {
    Image,
    Video
}