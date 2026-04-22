namespace FlowLens.Application.Features.Users.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string AvatarUrl,
    DateTimeOffset? LastLoginAt,
    UserSettingsDto? Settings
);