using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlowLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class UsersController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetMe()
    {
     
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User.Identity?.Name;

        return Ok(new
        {
            Message = "Gizli odaya hoş geldin agacım!",
            UserId = userId,
            Username = userName
        });
    }
}