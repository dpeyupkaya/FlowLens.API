using FlowLens.Application.Common.Interfaces; 
using FlowLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowLens.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    private readonly IEncryptionService _encryptionService;

    public UserConfiguration(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

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
            .HasMaxLength(500)
            .HasConversion(
                email => string.IsNullOrEmpty(email) ? null : _encryptionService.Encrypt(email),
                encryptedEmail => string.IsNullOrEmpty(encryptedEmail) ? null : _encryptionService.Decrypt(encryptedEmail)
            );

        builder.Property(u => u.GitHubAccessToken)
            .IsRequired()
            .HasMaxLength(1000)
            .HasConversion(
                token => _encryptionService.Encrypt(token),
                encryptedToken => _encryptionService.Decrypt(encryptedToken)
            );

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