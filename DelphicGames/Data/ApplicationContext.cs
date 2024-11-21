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
    public DbSet<StreamEntity> Streams { get; set; }
    public DbSet<Nomination> Nominations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Camera>()
        .HasOne(c => c.Nomination)
        .WithMany(n => n.Cameras)
        .HasForeignKey(c => c.NominationId)
        .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Camera>()
        .HasIndex(c => c.Name)
        .IsUnique();

        modelBuilder.Entity<Camera>()
        .HasIndex(c => c.Url)
        .IsUnique();

        modelBuilder.Entity<Nomination>()
        .HasIndex(n => n.Name)
        .IsUnique();

    }

    public void AddTestStreams()
    {
        Streams.RemoveRange(Streams);
        SaveChanges();

        var streams = new List<StreamEntity>();

        for (int i = 0; i < 40; i++)
        {
            Random rnd = new Random();

            var s = new StreamEntity
            {
                Day = rnd.Next(1, 10),
                StreamUrl = $"https://stream{i}",
                IsActive = false,
                Token = Guid.NewGuid().ToString(),
                PlatformName = "SSU",
                PlatformUrl = "https://ssu.com",
            };

            var nomination = Nominations.Skip(rnd.Next(Nominations.Count())).First();
            s.Nomination = nomination;

            streams.Add(s);
        }

        Streams.AddRange(streams);

        SaveChanges();
    }
}