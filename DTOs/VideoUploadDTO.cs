using System.ComponentModel.DataAnnotations;
using OpentubeAPI.Models;

namespace OpentubeAPI.DTOs;

public class VideoUploadDTO {
    [MaxLength(100)]
    public required string Title { get; set; }
    [MaxLength(2500)]
    public required string Description { get; set; }
    public Visibility Visibility { get; set; }
}