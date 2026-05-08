using FlowLens.Application.Features.GitHub.Queries.GetCSharpRepos;
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

    [HttpGet("csharp-repos")]
    public async Task<IActionResult> GetCSharpRepos()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
            throw new UnauthorizedAccessException("Kimlik bilginiz okunamadı, lütfen tekrar giriş yapın.");

        var query = new GetCSharpReposQuery(userId);

        var repos = await _mediator.Send(query);

        return Ok(repos);
    }
}