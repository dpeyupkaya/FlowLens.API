using FlowLens.Application.Interfaces.External;
using FlowLens.Domain.Repositories; 
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlowLens.Application.Features.GitHub.Queries.GetUserRepos;

public class GetUserReposQueryHandler : IRequestHandler<GetUserReposQuery, List<GitHubRepoResponse>>
{
    private readonly IGitHubService _gitHubService;
    private readonly IUserRepository _userRepository;

    public GetUserReposQueryHandler(IGitHubService gitHubService, IUserRepository userRepository)
    {
        _gitHubService = gitHubService;
        _userRepository = userRepository;
    }

    public async Task<List<GitHubRepoResponse>> Handle(GetUserReposQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user == null || string.IsNullOrEmpty(user.GitHubAccessToken))
        {
            throw new UnauthorizedAccessException("Geçersiz oturum veya GitHub bağlantısı bulunamadı.");
        }

        string visibilitySetting = user.Settings?.Data?.RepoVisibility ?? "All";

        var repos = await _gitHubService.GetUserReposAsync(user.GitHubAccessToken, visibilitySetting);
        
        if (repos == null) return new List<GitHubRepoResponse>();

        if (string.IsNullOrEmpty(request.Language) || request.Language.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return repos;
        }

        // Filter by the requested language
        return repos.Where(r => string.Equals(r.Language, request.Language, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}