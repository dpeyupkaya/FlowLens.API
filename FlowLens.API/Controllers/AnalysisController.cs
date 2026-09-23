using FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace FlowLens.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("GlobalIpPolicy")]
public class AnalysisController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalysisController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public record AnalyzeRequestDto(string RepoUrl, List<string>? IgnoredFolders, int? MaxDepth, int TimezoneOffsetMinutes, string AnalysisId, List<string>? TargetLanguages = null);

    [HttpPost("start")]
    public async Task<IActionResult> StartAnalysis([FromBody] AnalyzeRequestDto request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var command = new AnalyzeRepoCommand(
            request.RepoUrl,
            request.IgnoredFolders,
            request.MaxDepth,
            request.AnalysisId,
            request.TargetLanguages,
            request.TimezoneOffsetMinutes
        );

        var report = await _mediator.Send(command);

        return Ok(report);
    }
}