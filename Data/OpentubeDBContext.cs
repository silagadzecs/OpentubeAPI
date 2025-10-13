using Microsoft.EntityFrameworkCore;
using OpentubeAPI.Models;

namespace OpentubeAPI.Data;

public class OpentubeDBContext(DbContextOptions options) : DbContext(options) {
    public DbSet<User> Users { get; set; }
    public DbSet<VerificationCode> VerificationCodes { get; set; }
    public DbSet<UserRefreshToken>  UserRefreshTokens { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<VerificationCode>().HasKey(vc => new {
            vc.Email,
            vc.Code
        });
        modelBuilder.Entity<UserRefreshToken>().HasKey(ut => new {
            ut.UserId,
            ut.RefreshToken
        });
    }
}