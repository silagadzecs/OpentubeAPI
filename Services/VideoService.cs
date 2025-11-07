using System.Text;
using Castle.Components.DictionaryAdapter.Xml;
using FFMpegCore;
using OpentubeAPI.Data;
using OpentubeAPI.DTOs;
using OpentubeAPI.Models;
using OpentubeAPI.Utilities;
using Serilog;

namespace OpentubeAPI.Services;

public class VideoService(OpentubeDBContext context) {
    private static readonly string TempPath = Path.GetTempPath();

    public async Task<Result> Upload(IFormFile videoFile, IFormFile? thumbnail, VideoUploadDTO dto, string userId, CancellationToken cancellationToken) {
        var canceledError = new Result(new Error("Cancelled", "The HTTP Request was cancelled"));
        
        if (!videoFile.OpenReadStream().GetMimeType().StartsWith("video/")) {
            return new Result(new Error("Video", "Invalid video file"));
        }
        
        if (thumbnail is not null && !thumbnail.OpenReadStream().GetMimeType().StartsWith("image/")) {
            return new Result(new Error("Thumbnail", "Invalid thumbnail file"));
        }

        var videoTempPath = Path.Combine(TempPath, videoFile.FileName);
        await using var videoFs = new FileStream(videoTempPath, FileMode.Create);
        
        var cancelled = await videoFile.CopyToAsync(videoFs, cancellationToken).CatchCancellation(() => File.Delete(videoTempPath));
        if (cancelled) return canceledError;
        
        var videoId = GetBase64VideoId(11);
        var outputPath = Path.Combine(CDNService.VideoPath, videoId);
        Directory.CreateDirectory(outputPath);

        var videoInfo = await FFProbe.AnalyseAsync(videoTempPath, cancellationToken: cancellationToken).CatchCancellation(Cleanup);
        if (videoInfo is null) return canceledError;
        var videoHeight = videoInfo.PrimaryVideoStream?.Height ?? 0;

        try {
            var ffmpegArgs = FFMpegArguments
                .FromFileInput(videoTempPath)
                .OutputToFile(Path.Combine(outputPath, "manifest.mpd"), true, options =>
                    options
                        .AddBitrateArguments(videoHeight)
                        .WithCustomArgument("-map 0:a:0 -c:a aac -b:a 256k")
                        .WithCustomArgument("-g 50 -keyint_min 50 -err_detect ignore_err -threads 0")
                        .WithCustomArgument("-f dash -seg_duration 5 -use_timeline 1 -use_template 1")
                        .WithCustomArgument("-adaptation_sets \"id=0,streams=v id=1,streams=a\"")
                ).NotifyOnProgress(progress => {
                    Log.Information("{VideoId}: Processed {ProgressTotalSeconds} seconds out of {DurationTotalSeconds}", videoId, progress.TotalSeconds, videoInfo.Duration.TotalSeconds);
                }).CancellableThrough(cancellationToken);
            //Turns out it not working was a issue with ffmpeg itself and not an issue with my arguments, as ones taken
            //directly from the ffmpeg docs didn't work either. gotta stick to the slow software encoder for now ig
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
        
        if (thumbnail is not null) {
            var thumbnailExtension = Path.GetExtension(thumbnail.FileName);
            thumbnailFilename += thumbnailExtension;
            await using var thumbnailFs = new FileStream(Path.Combine(CDNService.ImagePath, thumbnailFilename), FileMode.Create);
            cancelled = await thumbnailFs.CopyToAsync(thumbnailFs, cancellationToken).CatchCancellation(Cleanup);
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
                Title = dto.Title,
                Description = dto.Description,
                ThumbnailFilename = thumbnailFilename,
                VideoFileId = videoMediaFile.Id
            }, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception e) {
            Log.Error(e, "Error saving video to database");
            Cleanup();
            return new Result(new Error("Video", "Video upload failed"));
        }
        try {File.Delete(videoTempPath);} catch { /* ignored */ }
        return new Result(videoId);

        void Cleanup() {
            try {
                File.Delete(videoTempPath);
                Extensions.DeleteNonEmptyDir(outputPath);
            } catch { /* Ignored */ }
        }
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