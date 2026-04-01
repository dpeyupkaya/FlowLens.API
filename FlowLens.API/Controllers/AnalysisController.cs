using FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace FlowLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDataProtector _protector;

    public AnalysisController(IMediator mediator, IDataProtectionProvider provider, IConfiguration configuration)
    {
        _mediator = mediator;

        var secretKey = configuration["SecuritySettings:CookieEncryptionKey"]
                        ?? throw new ArgumentNullException("CookieEncryptionKey eksik!");
        _protector = provider.CreateProtector(secretKey);
    }

    public record AnalyzeRequestDto(string RepoUrl);

    [HttpPost("start")]
    public async Task<IActionResult> StartAnalysis([FromBody] AnalyzeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RepoUrl))
            return BadRequest(new { Message = "Repo URL'si boş olamaz." });

        var encryptedToken = Request.Cookies["_fl_ctx_9x"];

        if (string.IsNullOrWhiteSpace(encryptedToken))
            return Unauthorized(new { Message = "Oturum bulunamadı. Lütfen giriş yap." });

        try
        {
            var githubToken = _protector.Unprotect(encryptedToken);

            var command = new AnalyzeRepoCommand(request.RepoUrl, githubToken);
            var report = await _mediator.Send(command);

            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Analiz sırasında bir hata oluştu.", Error = ex.Message });
        }
    }
}