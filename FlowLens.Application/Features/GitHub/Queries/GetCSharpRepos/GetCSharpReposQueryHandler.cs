using FlowLens.Application.Interfaces.External;
using MediatR;

namespace FlowLens.Application.Features.GitHub.Queries.GetCSharpRepos
{
    public class GetCSharpReposQueryHandler : IRequestHandler<GetCSharpReposQuery, List<GitHubRepoResponse>>
    {
        private readonly IGitHubService _gitHubService;

        public GetCSharpReposQueryHandler(IGitHubService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        public async Task<List<GitHubRepoResponse>> Handle(GetCSharpReposQuery request, CancellationToken cancellationToken)
        {
            var repos = await _gitHubService.GetUserCSharpReposAsync(request.AccessToken);

            return repos ?? new List<GitHubRepoResponse>();
        }
    }
}
