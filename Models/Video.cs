using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpentubeAPI.Models;

public class Video {
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid VideoFileId { get; init; }
    [MaxLength(100)]
    public required string Title { get; init; }
    [MaxLength(2500)]
    public required string Description { get; init; }
    [MaxLength(256)]
    public required string ThumbnailFilename { get; init; }
    
    public virtual MediaFile VideoFile { get; init; } = null!;
}