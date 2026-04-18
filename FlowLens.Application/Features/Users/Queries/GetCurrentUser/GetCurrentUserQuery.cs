using FlowLens.Application.Features.Users.DTOs;
using MediatR;
using System;

namespace FlowLens.Application.Features.Users.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto>;