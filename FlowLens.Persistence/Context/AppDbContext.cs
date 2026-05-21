using FlowLens.Domain.Entities;
using FlowLens.Persistence.Configurations;
using FlowLens.Application.Common.Interfaces; 
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FlowLens.Persistence.Context;

public class AppDbContext : DbContext
{
    private readonly IEncryptionService _encryptionService;

    public AppDbContext(DbContextOptions<AppDbContext> options, IEncryptionService encryptionService) : base(options)
    {
        _encryptionService = encryptionService;
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            t => t != typeof(UserConfiguration)
        );

        modelBuilder.ApplyConfiguration(new UserConfiguration(_encryptionService));

        base.OnModelCreating(modelBuilder);
    }
}