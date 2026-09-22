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
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public AuthController(IMediator mediator, IDataProtectionProvider provider, IConfiguration configuration, IWebHostEnvironment env)
    {
        _mediator = mediator;
        _configuration = configuration;
        _env = env;

        var secretKey = configuration["SecuritySettings:CookieEncryptionKey"]
                        ?? throw new InvalidOperationException("SecuritySettings:CookieEncryptionKey is missing in configuration!");
        _protector = provider.CreateProtector(secretKey);
    }

    public record GitHubLoginRequest(string Code, string State);

    [HttpGet("github-url")]
    public IActionResult GetGitHubLoginUrl()
    {
        var state = Guid.NewGuid().ToString("N");

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(10),
            Path = "/"
        };
        Response.Cookies.Append("oauth_state", state, cookieOptions);

        var clientId = _configuration["GitHub:ClientId"];
        var redirectUri = _configuration["GitHub:RedirectUri"];
        var githubAuthUrl = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&scope=repo,user&state={state}";

        return Ok(new { Url = githubAuthUrl });
    }

    [HttpPost("github-login")]
    public async Task<IActionResult> LoginWithGitHub([FromBody] GitHubLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.State))
        {
            return BadRequest(new { Message = "Güvenlik İhlali: State parametresi eksik." });
        }

        var savedState = Request.Cookies["oauth_state"];

        if (string.IsNullOrEmpty(savedState) || request.State != savedState)
        {
            Response.Cookies.Delete("oauth_state");
            return BadRequest(new { Message = "CRITICAL: Oturum süresi doldu veya geçersiz state!" });
        }

        Response.Cookies.Delete("oauth_state");

        var command = new LoginWithGitHubCommand(request.Code);
        var result = await _mediator.Send(command);

        if (!string.IsNullOrEmpty(result.Token))
        {
            var encryptedToken = _protector.Protect(result.Token);

            var authCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/"
            };

            Response.Cookies.Append("_fl_ctx_9x", encryptedToken, authCookieOptions);
        }

        return Ok(new { Message = "Giriş işlemi başarıyla tamamlandı." });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var jwtCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(-1),
            Path = "/"
        };
        Response.Cookies.Append("_fl_ctx_9x", "", jwtCookieOptions);

        return Ok(new { Message = "Oturum kapatıldı." });
    }
}