using System.Security;
using System.Security.Claims;
using System.Threading.RateLimiting;
using FFMpegCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpentubeAPI.Data;
using OpentubeAPI.Models;
using OpentubeAPI.Services;
using OpentubeAPI.Services.Interfaces;

namespace OpentubeAPI.Utilities;

public static class EnvKeys {
    public static class MailConfig {
        public const string Username = "MailConfig:Username";
        public const string Password = "MailConfig:Password";
        public const string SMTPServer = "MailConfig:SMTPServer";
        public const string SMTPPort = "MailConfig:SMTPPort";
    }

    public static class JwtConfig {
        public const string Secret = "JwtConfig:Secret";
        public const string Issuer = "JwtConfig:Issuer";
        public const string Audience = "JwtConfig:Audience";
        public const string AccessHours = "JwtConfig:AccessHours";
        public const string RefreshHours = "JwtConfig:RefreshHours";
    }

    public static class ConnectionStrings {
        public const string Default = "ConnectionStrings:Default";
    }
    public const string FFMpegPath = "FFMpegPath";
    public const string FilesDir = "FilesDir";
}
public static class ApiConfiguration {
    public static void CheckVariables(this IConfiguration conf) {
        List<string> variables = [
            EnvKeys.MailConfig.Username,
            EnvKeys.MailConfig.Password,
            EnvKeys.MailConfig.SMTPServer,
            EnvKeys.MailConfig.SMTPPort,
            EnvKeys.JwtConfig.Secret,
            EnvKeys.JwtConfig.Issuer,
            EnvKeys.JwtConfig.Audience,
            EnvKeys.JwtConfig.AccessHours,
            EnvKeys.JwtConfig.RefreshHours,
            EnvKeys.ConnectionStrings.Default,
            EnvKeys.FFMpegPath,
            EnvKeys.FilesDir
        ];
        List<string?> values = [];
        values.AddRange(variables.Select(conf.GetValue<string?>));

        if (!values.Any(v => v is null)) return;
        Console.WriteLine($"Environment Variables: {variables.ToCSVColumn()}" +
                          $"  need to be set in order for this app to function.\n" +
                          $"You are missing: {variables.Where((_, i) => values[i] is null).ToCSVColumn()}\n");
        try {
            Environment.Exit(1);
        } catch (SecurityException) {
            throw new Exception("Could not properly exit program");
        }
    }

    public static IServiceCollection ConfigureApp(this IServiceCollection services, IConfiguration conf) {
        services.AddCors(o => {
            o.AddDefaultPolicy(pol => {
                pol.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
            });
        });
        services.Configure<FormOptions>(opt => {
            opt.MultipartBodyLengthLimit = 10_737_418_240;
        });
        
        services.AddDbContext<OpentubeDBContext>(ob => {
            ob.UseLazyLoadingProxies().UseNpgsql(conf[EnvKeys.ConnectionStrings.Default]);
        });
        
        CDNService.SetPaths(conf[EnvKeys.FilesDir]!);
        var jwtConfig = conf.GetSection("JwtConfig").Get<JwtConfig>()!;
        var mailCreds =  conf.GetSection("MailConfig").Get<MailConfig>()!;
        services.AddDependencies(mailCreds, jwtConfig);
        services.AddJwtAuth(jwtConfig); 
        services.AddSwaggerGen(setup => {
            var jwtSecurityScheme = new OpenApiSecurityScheme {
                BearerFormat = "JWT",
                Name = "JWT Authentication",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                Description = "Access Token",

                Reference = new OpenApiReference {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };
            setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
            setup.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { jwtSecurityScheme, Array.Empty<string>() }
            });
         });
        services.AddRateLimiter(limiterOptions => limiterOptions
            .AddSlidingWindowLimiter(policyName: "slidingWindow", options =>
            {
                options.PermitLimit = 30;
                options.Window = TimeSpan.FromMinutes(1);
                options.SegmentsPerWindow = 10;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 10;
            }));
        GlobalFFOptions.Configure(options => options.BinaryFolder = conf[EnvKeys.FFMpegPath]!);
        return services;
    }

    private static void AddDependencies(this IServiceCollection services, MailConfig mailCreds, JwtConfig jwtConfig) {
        services.AddSingleton(mailCreds);
        services.AddSingleton(jwtConfig);
        services.AddScoped<MailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICDNService, CDNService>();
        services.AddScoped<IVideoService, VideoService>();
    }

    private static void AddJwtAuth(this IServiceCollection services, JwtConfig jwtConfig) {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => {
            o.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtConfig.Issuer,
                ValidAudience = jwtConfig.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromHexString(jwtConfig.Secret))
            };
            o.Events = new JwtBearerEvents {
                OnTokenValidated = async context => {
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId)) {
                        context.Fail("Invalid token: Missing userId ('sub' claim)");
                        return;
                    }
                    var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                    var exists = await authService.UserExistsAsync(userId);
                    var jti = context.Principal!.FindFirst(JwtRegisteredClaimNames.Jti)!.Value;
                    if (!exists) {
                        context.Fail("Invalid token: This token doesn't belong to a registered user");
                        return;
                    }

                    if (!await authService.IsAccessTokenValid(jti)) {
                        context.Fail("Invalid token");
                    }
                }
            };
        });
        services.AddAuthorization();
    }
}