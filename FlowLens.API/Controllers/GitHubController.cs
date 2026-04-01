using FlowLens.Application.Features.GitHub.Queries.GetCSharpRepos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace FlowLens.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GitHubController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IDataProtector _protector;

        public GitHubController(IMediator mediator, IDataProtectionProvider provider, IConfiguration configuration)
        {
            _mediator = mediator;

           
            var secretKey = configuration["SecuritySettings:CookieEncryptionKey"]
                            ?? throw new ArgumentNullException("CookieEncryptionKey eksik!");
            _protector = provider.CreateProtector(secretKey);
        }

        [HttpGet("csharp-repos")]
        public async Task<IActionResult> GetCSharpRepos() 
        {
          
            var encryptedToken = Request.Cookies["_fl_ctx_9x"];

            if (string.IsNullOrWhiteSpace(encryptedToken))
            {
                return Unauthorized(new { Message = "Oturum bulunamadı. Lütfen giriş yap agacım." });
            }

            try
            {
                
                var githubToken = _protector.Unprotect(encryptedToken);

                var query = new GetCSharpReposQuery(githubToken);

                var csharpRepos = await _mediator.Send(query);

                if (!csharpRepos.Any())
                {
                    return Ok(new { Message = "Bu hesaba ait public C# reposu bulunamadı.", Repos = csharpRepos });
                }

                return Ok(csharpRepos);
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, new { Message = "Repoları çekerken bir sorun oluştu.", Error = ex.Message });
            }
        }
    }
}