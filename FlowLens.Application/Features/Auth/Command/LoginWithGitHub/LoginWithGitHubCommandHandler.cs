using FlowLens.Application.Features.Auth.DTOs;
using FlowLens.Application.Interfaces.Auth;     
using FlowLens.Application.Interfaces.External; 
using FlowLens.Domain.Entities;
using FlowLens.Domain.Repositories;
using MediatR;

namespace FlowLens.Application.Features.Auth.Commands.LoginWithGitHub;

public class LoginWithGitHubCommandHandler : IRequestHandler<LoginWithGitHubCommand, AuthResponseDto>
{
    private readonly IGitHubService _githubService;
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;

    public LoginWithGitHubCommandHandler(
        IGitHubService githubService,
        ITokenService tokenService,
        IUserRepository userRepository)
    {
        _githubService = githubService;
        _tokenService = tokenService;
        _userRepository = userRepository;
    }

    public async Task<AuthResponseDto> Handle(LoginWithGitHubCommand request, CancellationToken cancellationToken)
    {
       
        var githubUser = await _githubService.GetUserInfoAsync(request.Code);

       
        var user = await _userRepository.GetByGitHubIdAsync(githubUser.Id.ToString());

        if (user == null)
        {
        
            user = new User
            {
                GitHubId = githubUser.Id.ToString(),
                Username = githubUser.Login,
                Email = githubUser.Email,
                AvatarUrl = githubUser.AvatarUrl,
                LastLoginAt = DateTimeOffset.UtcNow,
                DailyAnalysisCount = 0
            };

            await _userRepository.AddAsync(user);
        }
        else
        {
          
            user.LastLoginAt = DateTimeOffset.UtcNow;
            user.AvatarUrl = githubUser.AvatarUrl;

            await _userRepository.UpdateAsync(user);
        }

       
        var token = _tokenService.CreateToken(user);

        return new AuthResponseDto(
            token,
            user.Username,
            user.AvatarUrl,
            user.Email
        );
    }
}