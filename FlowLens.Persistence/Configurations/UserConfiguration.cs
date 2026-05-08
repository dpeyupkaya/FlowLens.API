using FlowLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowLens.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.GitHubId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.GitHubId)
            .IsUnique(); 

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(u => u.Email)
            .HasMaxLength(255);

        builder.Property(u => u.GitHubAccessToken)
            .IsRequired() 
            .HasMaxLength(500);

        builder.Property(u => u.DailyAnalysisCount)
            .HasDefaultValue(0);

        builder.OwnsOne(u => u.Settings, settingsBuilder =>
        {
            settingsBuilder.ToJson();

            settingsBuilder.OwnsOne(s => s.Analysis);
            settingsBuilder.OwnsOne(s => s.Graphics);
            settingsBuilder.OwnsOne(s => s.Data);
        });
    }
}