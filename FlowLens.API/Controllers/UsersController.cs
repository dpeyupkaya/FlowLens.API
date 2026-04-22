using FlowLens.Application.Features.Users.Queries.GetCurrentUser;
using FlowLens.Application.Features.Users.Commands.UpdateUserSettings;
using FlowLens.Application.Features.Users.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace FlowLens.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
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
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { Message = "Geçersiz token formatı." });
        }

        var query = new GetCurrentUserQuery(userId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    [HttpPut("me/settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UserSettingsDto settingsDto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { Message = "Geçersiz token formatı." });
        }

        var command = new UpdateUserSettingsCommand
        {
            UserId = userId,
            Settings = settingsDto
        };

        var isSuccess = await _mediator.Send(command);

        if (!isSuccess)
        {
            return NotFound(new { Message = "Kullanıcı bulunamadı veya güncellenemedi." });
        }

        return Ok(new { Message = "Ayarlar başarıyla güncellendi." });
    }
}