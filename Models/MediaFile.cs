using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace OpentubeAPI.Models;

[Index(nameof(Filename), IsUnique = true)]
public class MediaFile {
    public Guid Id { get; init; } = Guid.NewGuid();
    [MaxLength(256)]
    public required string Filename { get; init; }
    public required FileType FileType { get; init; }
    public required Guid OwnerId { get; init; }
    public required Visibility Visibility { get; init; }
    
    public virtual User Owner { get; init; } = null!;
}

public enum Visibility : byte {
    Public,
    Private
}

public enum FileType : byte {
    Image,
    Video
}