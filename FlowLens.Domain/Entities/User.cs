using FlowLens.Domain.Common;

namespace FlowLens.Domain.Entities;

public class User : BaseEntity
{
    public string GitHubId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string? Email { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public int DailyAnalysisCount { get; set; } = 0;
    public string GitHubAccessToken { get; set; } = string.Empty;
    public DateTime LastAnalysisDate { get; set; } = DateTime.UtcNow.Date;

    public UserSettings Settings { get; set; } = new();
}