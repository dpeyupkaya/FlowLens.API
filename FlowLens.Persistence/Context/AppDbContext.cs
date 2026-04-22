using FlowLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowLens.Persistence.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.GitHubId).IsUnique();

            entity.OwnsOne(u => u.Settings, settingsBuilder =>
            {
                settingsBuilder.ToJson(); 

                settingsBuilder.OwnsOne(s => s.Analysis);
                settingsBuilder.OwnsOne(s => s.Graphics);
                settingsBuilder.OwnsOne(s => s.Data);
            });
        });

        base.OnModelCreating(modelBuilder);
    }
}