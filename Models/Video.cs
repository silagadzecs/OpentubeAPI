using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpentubeAPI.Models;

public class Video {
    [Key, MaxLength(256)]
    public required string Id { get; init; }
    [MaxLength(100)]
    public required string Title { get; set; }
    [MaxLength(2500)]
    public required string Description { get; set; }
    [MaxLength(256)]
    public required string ThumbnailFilename { get; set; }
    
    [ForeignKey(nameof(Id))]
    public virtual MediaFile VideoFile { get; init; } = null!;
}