using FlowLens.Application.Interfaces.External;
using FlowLens.Domain.Repositories; 
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlowLens.Application.Features.GitHub.Queries.GetCSharpRepos;

public class GetCSharpReposQueryHandler : IRequestHandler<GetCSharpReposQuery, List<GitHubRepoResponse>>
{
    private readonly IGitHubService _gitHubService;
    private readonly IUserRepository _userRepository;

    public GetCSharpReposQueryHandler(IGitHubService gitHubService, IUserRepository userRepository)
    {
        _gitHubService = gitHubService;
        _userRepository = userRepository;
    }

    public async Task<List<GitHubRepoResponse>> Handle(GetCSharpReposQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user == null || string.IsNullOrEmpty(user.GitHubAccessToken))
        {
            throw new UnauthorizedAccessException("Geçersiz oturum veya GitHub bağlantısı bulunamadı.");
        }

        string visibilitySetting = user.Settings?.Data?.RepoVisibility ?? "All";

        var repos = await _gitHubService.GetUserReposAsync(user.GitHubAccessToken, visibilitySetting);

        return repos ?? new List<GitHubRepoResponse>();
    }
}