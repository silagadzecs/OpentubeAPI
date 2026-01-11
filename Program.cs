using System.Runtime.InteropServices;
using OpentubeAPI.Utilities;
using Serilog;
using Serilog.Filters;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Logger(lc => lc.Filter
        .ByExcluding(Matching.FromSource("FFMpegOut"))
        .WriteTo.File("./Logs/app.log", rollingInterval: RollingInterval.Day))
    .WriteTo.Logger(lc => lc.Filter
        .ByIncludingOnly(Matching.FromSource("FFMpegOut"))
        .WriteTo.File("./Logs/ffmpeg.log", rollingInterval: RollingInterval.Day))
    .CreateLogger();
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && builder.Configuration["ASPNETCORE_TEMP"] is null) 
    Log.Warning("If the system has low amounts of free ram (<11GiB) consider setting the ASPNETCORE_TEMP Environment variable to something other than /tmp");
    // I Think I've locked my self into linux anyway with the ffmpeg arguments, as VAAPI is only available on linux, so the os condition in the if may be a bit redundant
    // TODO: Do something about the aforementioned 

if (builder.Configuration["ASPNETCORE_TEMP"] is not null) Directory.CreateDirectory(builder.Configuration["ASPNETCORE_TEMP"]!);

builder.Configuration.CheckVariables();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureApp(builder.Configuration);

var app = builder.Build();
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();
Log.CloseAndFlush();