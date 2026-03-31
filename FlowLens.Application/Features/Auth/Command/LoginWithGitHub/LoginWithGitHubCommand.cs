using MediatR;
using FlowLens.Application.Features.Auth.DTOs;

namespace FlowLens.Application.Features.Auth.Commands.LoginWithGitHub;

public record LoginWithGitHubCommand(string Code) : IRequest<AuthResponseDto>;