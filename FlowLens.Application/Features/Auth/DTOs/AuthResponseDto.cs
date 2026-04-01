

namespace FlowLens.Application.Features.Auth.DTOs;

public record AuthResponseDto(
    string Token,
    string Username,
    string? AvatarUrl,
    string? Email,
    string GitHubAccessToken
);
