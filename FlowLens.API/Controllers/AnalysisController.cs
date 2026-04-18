using FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlowLens.Api.Controllers;

[Authorize] 
[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalysisController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public record AnalyzeRequestDto(string RepoUrl);

    [HttpPost("start")]
    public async Task<IActionResult> StartAnalysis([FromBody] AnalyzeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RepoUrl))
            return BadRequest(new { Message = "Repo URL'si boş olamaz." });

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized(new { Message = "Oturum bulunamadı. Lütfen giriş yap." });

        try
        {
            var command = new AnalyzeRepoCommand(request.RepoUrl, userId);
            var report = await _mediator.Send(command);

            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Analiz sırasında bir hata oluştu.", Error = ex.Message });
        }
    }
}