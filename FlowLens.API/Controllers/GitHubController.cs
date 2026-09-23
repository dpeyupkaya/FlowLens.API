using FlowLens.Application.Features.GitHub.Queries.GetUserRepos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace FlowLens.API.Controllers;

[Authorize] 
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("GlobalIpPolicy")]
public class GitHubController : ControllerBase
{
    private readonly IMediator _mediator;

    public GitHubController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("repos")]
    public async Task<IActionResult> GetUserRepos([FromQuery] string language = "All")
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
            throw new UnauthorizedAccessException("Kimlik bilginiz okunamadı, lütfen tekrar giriş yapın.");

        var query = new GetUserReposQuery(userId, language);

        var repos = await _mediator.Send(query);

        return Ok(repos);
    }

    [HttpGet("repo-languages")]
    public async Task<IActionResult> GetRepoLanguages([FromQuery] string repoUrl)
    {
        if (string.IsNullOrEmpty(repoUrl))
            return BadRequest("repoUrl parametresi gereklidir.");

        var query = new FlowLens.Application.Features.GitHub.Queries.GetRepoLanguages.GetRepoLanguagesQuery(repoUrl);
        var languages = await _mediator.Send(query);

        return Ok(languages);
    }
}