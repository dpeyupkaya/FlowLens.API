using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Application.Interfaces.External;
using FlowLens.Infrastructure.ExternalServices.GitHub.Models;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GitHubService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<(GitHubUserResponse User, string AccessToken)> GetUserAndTokenAsync(string code)
    {
        var tokenResponse = await GetAccessTokenAsync(code);
        var user = await GetUserFromGitHubAsync(tokenResponse.AccessToken);

        return (user, tokenResponse.AccessToken);
    }

    public async Task<List<GitHubRepoResponse>> GetUserReposAsync(string accessToken, string visibility = "all")
    {
        string githubVisibility = visibility.ToLower() switch
        {
            "public" => "public",
            "private" => "private",
            _ => "all"
        };

        var url = $"https://api.github.com/user/repos?visibility={githubVisibility}&per_page=100";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        request.Headers.UserAgent.ParseAdd("FlowLens-App");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var allRepos = await response.Content.ReadFromJsonAsync<List<GitHubRepoResponse>>()
                       ?? new List<GitHubRepoResponse>();

        return allRepos;
    }

    private async Task<GitHubTokenResponse> GetAccessTokenAsync(string code)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _configuration["GitHub:ClientId"]!),
            new KeyValuePair<string, string>("client_secret", _configuration["GitHub:ClientSecret"]!),
            new KeyValuePair<string, string>("code", code)
        });

        request.Content = content;
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();

        if (responseString.Contains("\"error\":"))
        {
            throw new Exception($"GitHub API Error: {responseString}");
        }

        return JsonSerializer.Deserialize<GitHubTokenResponse>(responseString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new Exception("Token deserialization failed.");
    }

    private async Task<GitHubUserResponse> GetUserFromGitHubAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        request.Headers.UserAgent.ParseAdd("FlowLens-App");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GitHubUserResponse>()
               ?? throw new Exception("User data deserialization failed.");
    }

    public async Task DownloadAndExtractRepoAsync(string repoUrl, string accessToken, string extractPath)
    {
        var uri = new Uri(repoUrl);
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length < 2)
            throw new Exception("Geçersiz GitHub URL'si .");

        var owner = segments[0];
        var repo = segments[1];

        var zipUrl = $"https://api.github.com/repos/{owner}/{repo}/zipball";

        var request = new HttpRequestMessage(HttpMethod.Get, zipUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        request.Headers.UserAgent.ParseAdd("FlowLens-App");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var tempZipPath = Path.Combine(extractPath, "repo.zip");

        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fileStream);
        }

        ZipFile.ExtractToDirectory(tempZipPath, extractPath, overwriteFiles: true);

        File.Delete(tempZipPath);
    }

    public async Task<RepoStatsDto> GetRepoStatsAsync(string repoUrl, string accessToken)
    {
        var uri = new Uri(repoUrl);
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length < 2)
            throw new Exception("Geçersiz GitHub URL'si.");

        var owner = segments[0];
        var repoName = segments[1].Replace(".git", ""); 

        var apiUrl = $"https://api.github.com/repos/{owner}/{repoName}";
        var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        }
        request.Headers.UserAgent.ParseAdd("FlowLens-App");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = jsonDoc.RootElement;

        return new RepoStatsDto(
            Stars: root.TryGetProperty("stargazers_count", out var stars) ? stars.GetInt32() : 0,
            Forks: root.TryGetProperty("forks_count", out var forks) ? forks.GetInt32() : 0,
            OpenIssues: root.TryGetProperty("open_issues_count", out var issues) ? issues.GetInt32() : 0,
            PrimaryLanguage: root.TryGetProperty("language", out var lang) && lang.ValueKind != JsonValueKind.Null ? lang.GetString()! : "Bilinmiyor",
            CreatedAt: root.TryGetProperty("created_at", out var createdAt) ? createdAt.GetDateTime() : DateTime.MinValue,
            LastPushedAt: root.TryGetProperty("pushed_at", out var pushedAt) ? pushedAt.GetDateTime() : DateTime.MinValue,
            DefaultBranch: root.TryGetProperty("default_branch", out var branch) ? branch.GetString()! : "main"
        );
    }
}