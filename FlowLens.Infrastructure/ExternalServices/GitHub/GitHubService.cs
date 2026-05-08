using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Application.Interfaces.External;
using FlowLens.Infrastructure.ExternalServices.GitHub.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

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

    public async Task<(bool IsAccessible, bool IsPrivate)> VerifyRepoAccessAsync(string repoUrl, string accessToken)
    {
        try
        {
            var uri = new Uri(repoUrl);
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length < 2)
                return (false, false);

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

            if (response.IsSuccessStatusCode)
            {
                var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var isPrivate = jsonDoc.RootElement.TryGetProperty("private", out var privateProp) && privateProp.GetBoolean();

                return (true, isPrivate);
            }

            return (false, false);
        }
        catch
        {
            return (false, false);
        }
    }

    public async Task DownloadAndExtractRepoAsync(string repoUrl, string accessToken, string extractPath)
    {
        var uri = new Uri(repoUrl);
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length < 2)
            throw new Exception("Geçersiz GitHub URL'si.");

        var owner = segments[0];
        var repo = segments[1].Replace(".git", "");

        var zipUrl = $"https://api.github.com/repos/{owner}/{repo}/zipball";

        var request = new HttpRequestMessage(HttpMethod.Get, zipUrl);
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        }
        request.Headers.UserAgent.ParseAdd("FlowLens-App");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        const long maxZipSizeBytes = 50 * 1024 * 1024; 
        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength.HasValue && contentLength.Value > maxZipSizeBytes)
        {
            throw new InvalidOperationException($"Depo boyutu çok büyük ({(contentLength.Value / 1024 / 1024)} MB). Sistem güvenliği gereği maksimum 50 MB boyutundaki depolar analiz edilebilir.");
        }

        var tempZipPath = Path.Combine(extractPath, "repo.zip");

        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            byte[] buffer = new byte[8192];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                totalBytesRead += bytesRead;
                if (totalBytesRead > maxZipSizeBytes)
                {
                    throw new InvalidOperationException("Depo indirme limiti aşıldı. Dosya çok büyük.");
                }
                await fileStream.WriteAsync(buffer, 0, bytesRead);
            }
        }

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".csproj", ".sln", ".json", ".xml", ".md", ".txt"
        };

        long totalExtractedSize = 0;
        const long maxExtractedSizeBytes = 150 * 1024 * 1024; 

        using (var archive = ZipFile.OpenRead(tempZipPath))
        {
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;

                var extension = Path.GetExtension(entry.Name);

                if (!allowedExtensions.Contains(extension)) continue;

                totalExtractedSize += entry.Length;
                if (totalExtractedSize > maxExtractedSizeBytes)
                {
                    throw new InvalidOperationException("Çıkartılan dosyaların toplam boyutu güvenlik limitini aştı.");
                }

                var destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                var extractFullPath = Path.GetFullPath(extractPath + Path.DirectorySeparatorChar);

                if (!destinationPath.StartsWith(extractFullPath, StringComparison.Ordinal))
                {
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.ExtractToFile(destinationPath, overwrite: true);
            }
        }

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
}