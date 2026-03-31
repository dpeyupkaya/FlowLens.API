
namespace FlowLens.Application.Interfaces.External
{
    public interface IGitHubService
    {
        Task<GitHubUserResponse> GetUserInfoAsync(string code);
    }
}
