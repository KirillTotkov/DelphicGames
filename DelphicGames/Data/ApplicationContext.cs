using DelphicGames.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Data;

public class ApplicationContext : IdentityDbContext<User>
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
        EnsurePlatforms();
    }

    public DbSet<Camera> Cameras { get; set; }
    public DbSet<Platform> Platforms { get; set; }
    public DbSet<CameraPlatform> CameraPlatforms { get; set; }
    public DbSet<Nomination> Nominations { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Region> Regions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Связь многие ко многим между камерами и платформами
        modelBuilder.Entity<CameraPlatform>()
            .HasKey(cp => new { cp.CameraId, cp.PlatformId }); // Составной ключ

        modelBuilder.Entity<CameraPlatform>()
            .HasOne(cp => cp.Camera)
            .WithMany(c => c.CameraPlatforms)
            .HasForeignKey(cp => cp.CameraId);

        modelBuilder.Entity<CameraPlatform>()
            .HasOne(cp => cp.Platform)
            .WithMany(p => p.CameraPlatforms)
            .HasForeignKey(cp => cp.PlatformId);

        modelBuilder.Entity<Camera>()
            .HasOne(c => c.Nomination)
            .WithMany(n => n.Cameras)
            .HasForeignKey(c => c.NominationId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private void EnsurePlatforms()
    {
        var platforms = new List<Platform>
        {
            new() { Name = "ВК", Url = "rtmp://ovsu.mycdn.me/input/"},
            new() { Name = "ОК", Url = "rtmp://vsu.mycdn.me/input/"},
            new() { Name = "RT", Url = "rtmp://rtmp-lb.m9.rutube.ru/live_push"},
            new() { Name = "TG", Url = "rtmps://dc4-1.rtmp.t.me/s/"},
        };

        foreach (var platform in platforms)
        {
            if (!Platforms.Any(p => p.Name == platform.Name))
            {
                Platforms.Add(platform);
            }
        }

        SaveChanges();
    }
}