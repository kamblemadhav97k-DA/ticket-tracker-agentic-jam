using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketTracker.Application.Teams;

namespace TicketTracker.API.Controllers;

[ApiController]
[Authorize]
[Route("api/teams")]
public class TeamsController(ITeamService teams) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> GetAll(CancellationToken ct) =>
        Ok(await teams.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeamDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await teams.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<TeamDto>> Create(CreateTeamRequest request, CancellationToken ct)
    {
        var team = await teams.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = team.Id }, team);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TeamDto>> Update(Guid id, UpdateTeamRequest request, CancellationToken ct) =>
        Ok(await teams.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await teams.DeleteAsync(id, ct);
        return NoContent();
    }
}
