using FlowLens.Application.Interfaces;
using FlowLens.Application.Interfaces.External;
using FlowLens.Domain.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlowLens.Application.Features.GitHub.Queries.GetRepoLanguages;

public class GetRepoLanguagesQueryHandler : IRequestHandler<GetRepoLanguagesQuery, Dictionary<string, int>>
{
    private readonly IGitHubService _gitHubService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;

    public GetRepoLanguagesQueryHandler(
        IGitHubService gitHubService, 
        ICurrentUserService currentUserService,
        IUserRepository userRepository)
    {
        _gitHubService = gitHubService;
        _currentUserService = currentUserService;
        _userRepository = userRepository;
    }

    public async Task<Dictionary<string, int>> Handle(GetRepoLanguagesQuery request, CancellationToken cancellationToken)
    {
        var userIdString = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userGuid))
        {
            throw new UnauthorizedAccessException("Oturum bilgisi doğrulanamadı.");
        }

        var user = await _userRepository.GetByIdAsync(userGuid);
        return await _gitHubService.GetRepoLanguagesAsync(request.RepoUrl, user.GitHubAccessToken);
    }
}
