using FlowLens.Application.Features.GitHub.Queries.GetCSharpRepos;
using FlowLens.Domain.Repositories;
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
        private readonly IUserRepository _userRepository;

        public GitHubController(IMediator mediator, IUserRepository userRepository)
        {
            _mediator = mediator;
            _userRepository = userRepository;
        }

        [HttpGet("csharp-repos")]
        public async Task<IActionResult> GetCSharpRepos()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "Kimliğin doğrulanamadı agacım." });

            var user = await _userRepository.GetByIdAsync(Guid.Parse(userIdString));

            if (user == null || string.IsNullOrEmpty(user.GitHubAccessToken))
            {
                return BadRequest(new { Message = "Sistemde GitHub bağlantın bulunamadı." });
            }

            try
            {
                var query = new GetCSharpReposQuery(user.GitHubAccessToken);
                var repos = await _mediator.Send(query);

                return Ok(repos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "GitHub ile konuşurken bir sorun oldu.", Error = ex.Message });
            }
        }
    }
}