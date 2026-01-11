using System.Text;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using OpentubeAPI.Data;
using OpentubeAPI.DTOs;
using OpentubeAPI.Models;
using OpentubeAPI.Services.Interfaces;
using OpentubeAPI.Utilities;
using Serilog;
using Serilog.Core;
using ILogger = Serilog.ILogger;

namespace OpentubeAPI.Services;

public class VideoService(OpentubeDBContext context) : IVideoService {
    private static readonly string TempPath = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? Path.GetTempPath();
    private static readonly ILogger FFMpegLogger = Log.ForContext(Constants.SourceContextPropertyName, "FFMpegOut");

    public async Task<Result> Upload(VideoUploadDTO dto, string userId, CancellationToken cancellationToken) {
        var canceledError = new Result(new Error("Cancelled", "The HTTP Request was cancelled"));
        
        if (!dto.Video.OpenReadStream().GetMimeType().StartsWith("video/")) {
            return new Result(new Error("Video", "Invalid video file"));
        }
        
        if (dto.Thumbnail is not null && !dto.Thumbnail.OpenReadStream().GetMimeType().StartsWith("image/")) {
            return new Result(new Error("Thumbnail", "Invalid thumbnail file"));
        }

        var videoTempPath = Path.Combine(TempPath, dto.Video.FileName);
        await using var videoFs = new FileStream(videoTempPath, FileMode.Create);
        
        var cancelled = await dto.Video.CopyToAsync(videoFs, cancellationToken).CatchCancellation(() => File.Delete(videoTempPath));
        if (cancelled) return canceledError;
        
        var videoId = GetBase64VideoId(11);
        var outputPath = Path.Combine(CDNService.VideoPath, videoId);
        while (Directory.Exists(outputPath)) {
            videoId = GetBase64VideoId(11);
            outputPath = Path.Combine(CDNService.VideoPath, videoId);
        }
        Directory.CreateDirectory(outputPath);

        var videoInfo = await FFProbe.AnalyseAsync(videoTempPath, cancellationToken: cancellationToken).CatchCancellation(Cleanup);
        if (videoInfo is null) return canceledError;
        var videoHeight = videoInfo.PrimaryVideoStream?.Height ?? 0;

        try {
            var ffmpegArgs = FFMpegArguments
                .FromFileInput(videoTempPath)
                .OutputToFile(
                    Path.Combine(outputPath, "manifest.mpd"),
                    true,
                    options =>
                    options
                        .WithCustomArgument("-v fatal -stats") //Makes FFmpeg output only stats and fatal errors
                        .AddVAAPIArguments()
                        .AddBitrateArguments(videoHeight)
                        .WithCustomArgument("-map 0:a? -c:a aac -b:a 256k")
                        .AddDASHArguments()
                ).NotifyOnProgress(progress => {
                    FFMpegLogger.Information("{VideoId}: Processed {ProgressTotalSeconds} seconds out of {DurationTotalSeconds}",
                        videoId, progress.TotalSeconds, videoInfo.Duration.TotalSeconds);
                }).NotifyOnError(progress => {
                    FFMpegLogger.Information("FFMpeg output: {progress}", progress); 
                    //FFmpeg outputs all text to stderr as stdout is reserved for the actual files 
                }).CancellableThrough(cancellationToken);
            Log.Information("FFMpeg running with args: {ffmpegArgs}", ffmpegArgs.Arguments);
            var success = await ffmpegArgs.ProcessAsynchronously();
            if (!success) {
                Cleanup();
                return new Result(new Error("Video", "Video processing failed"));
            }
        } 
        catch (Exception ex) {
            if (ex is not OperationCanceledException) 
                Log.Error(ex, "Error processing video {videoId}", videoId);
            Cleanup();
            return ex is OperationCanceledException 
                ? canceledError 
                : new Result(new Error("Video", "Video processing failed"));
        }

        var thumbnailFilename = $"thumbnail_{videoId}";
        
        if (dto.Thumbnail is not null) {
            var thumbnailExtension = Path.GetExtension(dto.Thumbnail.FileName).ToLower();
            thumbnailFilename += thumbnailExtension;
            await using var thumbnailFs = new FileStream(Path.Combine(CDNService.ImagePath, thumbnailFilename), FileMode.Create);
            cancelled = await dto.Thumbnail.CopyToAsync(thumbnailFs, cancellationToken).CatchCancellation(Cleanup);
            if (cancelled) return canceledError;
        } else {
            try {
                thumbnailFilename += ".png";
                await FFMpeg.SnapshotAsync(videoTempPath, Path.Combine(CDNService.ImagePath, thumbnailFilename),
                    captureTime: videoInfo.Duration / 2);
            } catch (Exception e) {
                Log.Warning(e, "Failed to generate thumbnail for {videoId}", videoId);
            }
        }

        try {
            var thumbnailMediaFile = new MediaFile {
                Filename = thumbnailFilename,
                FileType = FileType.Image,
                OwnerId = userId.ToGuid(),
                Visibility = dto.Visibility
            };
            var videoMediaFile = new MediaFile {
                Filename = videoId,
                FileType = FileType.Video,
                OwnerId = userId.ToGuid(),
                Visibility = dto.Visibility
            };
            await context.MediaFiles.AddAsync(thumbnailMediaFile, CancellationToken.None);
            await context.MediaFiles.AddAsync(videoMediaFile, CancellationToken.None);
            await context.Videos.AddAsync(new Video {
                Id = videoMediaFile.Filename,
                Title = dto.Title,
                Description = dto.Description,
                ThumbnailFilename = thumbnailFilename,
            }, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception e) {
            Log.Error(e, "Error saving video to database");
            Cleanup();
            return new Result(new Error("Video", "Video upload failed"));
        }
        try { File.Delete(videoTempPath);} catch { /* ignored */ }
        return new Result(new {videoId});

        void Cleanup() {
            try {
                File.Delete(videoTempPath);
                Extensions.DeleteNonEmptyDir(outputPath);
            } catch { /* Ignored */ }
        }
    }

    public async Task<Result> GetVideos(string? userId) {
        var videos = await context.Videos
            .Include(v => v.VideoFile)
            .ThenInclude(vf => vf.Owner)
            .Where(v => v.VideoFile.Visibility == Visibility.Public || v.VideoFile.OwnerId == userId.ToGuid()).ToListAsync();
        return new Result(videos.Select(v => new VideoDTO(v)));
    }

    public async Task<Result> GetVideo(string videoId, string? userId) {
        var video = await context.Videos
            .Include(v => v.VideoFile)
            .ThenInclude(vf => vf.Owner)
            .FirstOrDefaultAsync(v => v.Id == videoId && 
                (v.VideoFile.Visibility == Visibility.Public || v.VideoFile.OwnerId == userId.ToGuid()));
        return video is not null ? new Result(new VideoDTO(video)) : new Result(new Error("videoId", "Video not found"));
    }

    public async Task<Result> DeleteVideo(string videoId, string userId) {
        var video = await context.Videos
            .Include(v => v.VideoFile)
            .FirstOrDefaultAsync(v => v.Id == videoId);
        if (video is null) return new Result(new Error("videoId", "Video not found")) {
            StatusCode = 404
        };
        if (video.VideoFile.OwnerId != userId.ToGuid()) return new Result(new Error("User", "Forbidden")) {
            StatusCode = 403
        };

        try {
            Extensions.DeleteNonEmptyDir(Path.Combine(CDNService.VideoPath, videoId));
        }
        catch {
            // ignored
        }
        finally {
            context.MediaFiles.Remove(video.VideoFile);
            await context.SaveChangesAsync();
        }
        return new Result($"Successfully deleted {videoId}");
    }

    public async Task<Result> EditVideo(string videoId, VideoEditDTO dto, string userId) {
        if (dto.Thumbnail is not null && !dto.Thumbnail.OpenReadStream().GetMimeType().StartsWith("image/")) {
            return new Result(new Error("Thumbnail", "Invalid thumbnail file"));
        }
        var video = await context.Videos
            .Include(v => v.VideoFile)
            .FirstOrDefaultAsync(v => v.Id == videoId);
        if (video is null) return new Result(new Error("videoId", "Video not found")) {
            StatusCode = 404
        };
        if (video.VideoFile.OwnerId != userId.ToGuid()) return new Result(new Error("User", "Forbidden")) {
            StatusCode = 403
        };
        var thumbnailFilename = "";
        if (dto.Thumbnail is not null) {
            var thumbnailExtension = Path.GetExtension(dto.Thumbnail.FileName).ToLower();
            thumbnailFilename = $"thumbnail_{videoId}.{thumbnailExtension}";
            var oldNameIsEqualToNew = thumbnailFilename == video.ThumbnailFilename;
            var thumbnailPath = Path.Combine(CDNService.ImagePath, thumbnailFilename);
            try {
                await using var fs = new FileStream(thumbnailPath, FileMode.Create);
                await dto.Thumbnail.CopyToAsync(fs);
            }
            catch (Exception e) {
                Log.Error(e, "Error changing thumbnail for video {id}", videoId);
                return new Result(new Error("Thumbnail", "Unable to change thumbnail for video")) {
                    StatusCode = 500
                };
            }
            
            if(!oldNameIsEqualToNew)
                try {
                    File.Delete(Path.Combine(CDNService.ImagePath, video.ThumbnailFilename));
                }
                catch {
                    // ignored
                }
        }

        video.Title = dto.Title;
        video.Description = dto.Description;
        video.VideoFile.Visibility = dto.Visibility;
        video.ThumbnailFilename = thumbnailFilename;
        await context.SaveChangesAsync();
        return new Result("Successfully updated video");

    }

    private static string GetBase64VideoId(int length) {
        const string urlSafeB64Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_";
        var sb = new StringBuilder();
        var random = new Random();
        for (var i = 0; i < length; i++) {
            sb.Append(urlSafeB64Chars[random.Next(0, urlSafeB64Chars.Length)]);
        }

        return sb.ToString();
    }
}