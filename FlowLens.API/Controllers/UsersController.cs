using FlowLens.Application.Features.Users.Queries.GetCurrentUser;
using FlowLens.Application.Features.Users.Commands.UpdateUserSettings;
using FlowLens.Application.Features.Users.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace FlowLens.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("GlobalIpPolicy")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var query = new GetCurrentUserQuery(userId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    [HttpPut("me/settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UserSettingsDto settingsDto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var command = new UpdateUserSettingsCommand
        {
            UserId = userId,
            Settings = settingsDto
        };

        await _mediator.Send(command);

        return Ok(new { Message = "Ayarlarınız başarıyla  işlendi." });
    }
}