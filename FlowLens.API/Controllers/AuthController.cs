using FlowLens.Application.Features.Auth.Commands.LoginWithGitHub;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace FlowLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDataProtector _protector;

    public AuthController(IMediator mediator, IDataProtectionProvider provider, IConfiguration configuration)
    {
        _mediator = mediator;
        var secretKey = configuration["SecuritySettings:CookieEncryptionKey"]
                        ?? throw new ArgumentNullException("CookieEncryptionKey eksik!");
        _protector = provider.CreateProtector(secretKey);
    }

    [HttpPost("github-login")]
    public async Task<IActionResult> LoginWithGitHub([FromBody] string code)
    {
        if (string.IsNullOrEmpty(code))
            return BadRequest("Kod boş.");

        var command = new LoginWithGitHubCommand(code);
        var result = await _mediator.Send(command);

        if (!string.IsNullOrEmpty(result.Token))
        {
            var encryptedToken = _protector.Protect(result.GitHubAccessToken);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("_fl_ctx_9x", encryptedToken, cookieOptions);
        }
        return Ok(new
        {
            Message = "Giriş başarılı!",
         });

    }
}