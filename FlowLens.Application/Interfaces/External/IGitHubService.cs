using FlowLens.Application.Features.Analysis.DTOs;

namespace FlowLens.Application.Interfaces.External
{
    public interface IGitHubService
    {
        Task<(GitHubUserResponse User, string AccessToken)> GetUserAndTokenAsync(string code);
        Task<List<GitHubRepoResponse>> GetUserReposAsync(string accessToken, string visibility = "all");
        Task DownloadAndExtractRepoAsync(string repoUrl, string accessToken, string extractPath);

        Task<RepoStatsDto> GetRepoStatsAsync(string repoUrl, string accessToken);
    }
}