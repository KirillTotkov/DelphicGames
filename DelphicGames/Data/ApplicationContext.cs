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
    }
}