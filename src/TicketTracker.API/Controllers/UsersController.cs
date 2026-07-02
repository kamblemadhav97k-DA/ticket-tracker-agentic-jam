using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketTracker.Application.Users;

namespace TicketTracker.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(IUserDirectory users) : ControllerBase
{
    /// <summary>All registered users, for assignee pickers.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken ct) =>
        Ok(await users.ListAsync(ct));
}
