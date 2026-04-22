using FlowLens.Application.Features.Users.DTOs;
using MediatR;

namespace FlowLens.Application.Features.Users.Commands.UpdateUserSettings;

public class UpdateUserSettingsCommand : IRequest<bool>
{
    public Guid UserId { get; set; }

    public UserSettingsDto Settings { get; set; } = new();
}