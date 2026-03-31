using FlowLens.Application.Features.Auth.Commands.LoginWithGitHub;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("github-login")]
    public async Task<IActionResult> LoginWithGitHub([FromBody] string code)
    {
        if (string.IsNullOrEmpty(code))
            return BadRequest("Kod boş olamaz agacım.");

        var command = new LoginWithGitHubCommand(code);
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}