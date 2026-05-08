using FlowLens.Application.Features.Auth.Commands.LoginWithGitHub;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;

namespace FlowLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("GlobalIpPolicy")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDataProtector _protector;

    public AuthController(IMediator mediator, IDataProtectionProvider provider, IConfiguration configuration)
    {
        _mediator = mediator;
        var secretKey = configuration["SecuritySettings:CookieEncryptionKey"]
                        ?? throw new InvalidOperationException("SecuritySettings:CookieEncryptionKey is missing in configuration!");
        _protector = provider.CreateProtector(secretKey);
    }

    [HttpPost("github-login")]
    public async Task<IActionResult> LoginWithGitHub([FromBody] string code)
    {
       

        var command = new LoginWithGitHubCommand(code);
        var result = await _mediator.Send(command);

        if (!string.IsNullOrEmpty(result.Token))
        {
            var encryptedToken = _protector.Protect(result.Token);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,   
                Secure = true,    
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/" 
            };

            Response.Cookies.Append("_fl_ctx_9x", encryptedToken, cookieOptions);
        }

        return Ok(new { Message = "Giriş işlemi başarıyla tamamlandı." });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("_fl_ctx_9x");
        return Ok(new { Message = "Oturum kapatıldı." });
    }
}