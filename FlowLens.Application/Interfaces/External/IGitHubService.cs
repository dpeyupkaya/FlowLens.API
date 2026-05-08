using FlowLens.Application.Features.Analysis.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FlowLens.Application.Interfaces.External
{
    public interface IGitHubService
    {
        Task<(GitHubUserResponse User, string AccessToken)> GetUserAndTokenAsync(string code);
        Task<List<GitHubRepoResponse>> GetUserReposAsync(string accessToken, string visibility = "all");

        Task<(bool IsAccessible, bool IsPrivate)> VerifyRepoAccessAsync(string repoUrl, string accessToken);

        Task DownloadAndExtractRepoAsync(string repoUrl, string accessToken, string extractPath);
        Task<RepoStatsDto> GetRepoStatsAsync(string repoUrl, string accessToken);
    }
}