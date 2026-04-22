using FlowLens.Application.Features.GitHub.Queries.GetCSharpRepos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlowLens.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "Kimliğin doğrulanamadı agacım." });

            var userId = Guid.Parse(userIdString);

            try
            {
                var query = new GetCSharpReposQuery(userId);

                var repos = await _mediator.Send(query);

                return Ok(repos);
            }
            catch (UnauthorizedAccessException ex)
            {
             
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "GitHub ile konuşurken bir sorun oldu.", Error = ex.Message });
            }
        }
    }
}