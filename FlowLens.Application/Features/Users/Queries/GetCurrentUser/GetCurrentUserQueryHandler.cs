using FlowLens.Application.Features.Users.DTOs;
using FlowLens.Domain.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlowLens.Application.Features.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Geçersiz oturum: Kullanıcı bulunamadı.");
        }

        return new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.AvatarUrl,
            user.LastLoginAt
        );
    }
}