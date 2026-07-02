using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketTracker.Application.Epics;

namespace TicketTracker.API.Controllers;

[ApiController]
[Authorize]
[Route("api/epics")]
public class EpicsController(IEpicService epics) : ControllerBase
{
    /// <summary>Lists epics, optionally filtered to a single team.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EpicDto>>> GetAll([FromQuery] Guid? teamId, CancellationToken ct) =>
        Ok(await epics.GetAllAsync(teamId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EpicDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await epics.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<EpicDto>> Create(CreateEpicRequest request, CancellationToken ct)
    {
        var epic = await epics.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = epic.Id }, epic);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EpicDto>> Update(Guid id, UpdateEpicRequest request, CancellationToken ct) =>
        Ok(await epics.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await epics.DeleteAsync(id, ct);
        return NoContent();
    }
}
