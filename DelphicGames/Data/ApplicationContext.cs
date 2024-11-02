using DelphicGames.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Data;

public class ApplicationContext : IdentityDbContext<User>
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
    }

    public DbSet<Camera> Cameras { get; set; }
    public DbSet<Platform> Platforms { get; set; }
    public DbSet<CameraPlatforms> CameraPlatforms { get; set; }
    public DbSet<Nomination> Nominations { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Region> Regions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Связь многие ко многим между камерами и платформами
        modelBuilder.Entity<CameraPlatforms>()
            .HasKey(cp => new { cp.CameraId, cp.PlatformId }); // Составной ключ

        modelBuilder.Entity<CameraPlatforms>()
            .HasOne(cp => cp.Camera)
            .WithMany(c => c.CameraPlatforms)
            .HasForeignKey(cp => cp.CameraId);

        modelBuilder.Entity<CameraPlatforms>()
            .HasOne(cp => cp.Platform)
            .WithMany(p => p.CameraPlatforms)
            .HasForeignKey(cp => cp.PlatformId);
    }
}