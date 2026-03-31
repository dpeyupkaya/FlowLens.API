using FlowLens.Application.Interfaces.External;
using FlowLens.Infrastructure.ExternalServices.GitHub.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
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

    public async Task<GitHubUserResponse> GetUserInfoAsync(string code)
    {
        var tokenResponse = await GetAccessTokenAsync(code);
        return await GetUserFromGitHubAsync(tokenResponse.AccessToken);
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