using System.Security;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpentubeAPI.Data;
using OpentubeAPI.Models;
using OpentubeAPI.Services;

namespace OpentubeAPI.Utilities;

public static class ApiConfiguration {
    public static void CheckVariables(this IConfiguration conf) {
        List<string> variables = [
            "MailConfig:Username",
            "MailConfig:Password",
            "MailConfig:SMTPServer",
            "MailConfig:SMTPPort",
            "JwtConfig:Secret",
            "JwtConfig:Issuer",
            "JwtConfig:Audience",
            "JwtConfig:AccessHours",
            "JwtConfig:RefreshHours",
            "SqlServerConnectionString",
            "FFMpegPath",
        ];
        List<string?> values = [];
        values.AddRange(variables.Select(conf.GetValue<string?>));

        if (!values.Any(v => v is null)) return;
        Console.WriteLine($"Environment Variables: {variables.ToCSVColumn()}" +
                          $"  need to be set in order for this app to function.\n" +
                          $"You are missing: {variables.Where((_, i) => values[i] is null).ToCSVColumn()}\n");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
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
        
        services.AddDbContext<OpentubeDBContext>(ob => {
            ob.UseSqlServer(conf["SqlServerConnectionString"]);
        });
        
        var jwtConfig = conf.GetSection("JwtConfig").Get<JwtConfig>()!;
        var mailCreds =  conf.GetSection("MailConfig").Get<MailConfig>()!;
        services.AddDependencies(mailCreds, jwtConfig);
        services.AddJwtAuth(jwtConfig);
        services.AddHttpClient();
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

        return services;
    }

    private static void AddDependencies(this IServiceCollection services, MailConfig mailCreds, JwtConfig jwtConfig) {
        services.AddScoped<MailConfig>(_ => mailCreds);
        services.AddScoped<JwtConfig>(_ => jwtConfig);
        services.AddScoped<MailService>();
        services.AddScoped<AuthService>();
        services.AddScoped<CDNService>();
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
                    var authService = context.HttpContext.RequestServices.GetRequiredService<AuthService>();
                    var exists = await authService.UserExistsAsync(userId);
                    var jti = context.Principal!.FindFirst(JwtRegisteredClaimNames.Jti)!.Value;
                    if (!exists) {
                        context.Fail("Invalid token: This token doesn't belong to a registered user");
                        return;
                    }

                    if (!await authService.AccessTokenValid(jti)) {
                        context.Fail("Invalid token");
                    }
                }
            };
        });
        services.AddAuthorization();
    }
}