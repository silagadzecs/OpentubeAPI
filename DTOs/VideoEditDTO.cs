using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OpentubeAPI.Models;

namespace OpentubeAPI.DTOs;

public class VideoEditDTO {
    [FromForm, MaxLength(100)]
    public required string Title { get; set; }
    [FromForm, MaxLength(2500)]
    public required string Description { get; set; }
    [FromForm]
    public required Visibility Visibility { get; set; }
    public IFormFile? Thumbnail { get; set; }
}