using System.ComponentModel.DataAnnotations;

namespace TicketTracker.Application.Teams;

public record TeamDto(
    Guid Id, string Name, int TicketCount, int EpicCount, DateTime CreatedAt, DateTime ModifiedAt);

public record CreateTeamRequest([Required] string Name);

public record UpdateTeamRequest([Required] string Name);

public interface ITeamService
{
    Task<IReadOnlyList<TeamDto>> GetAllAsync(CancellationToken ct = default);
    Task<TeamDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TeamDto> CreateAsync(CreateTeamRequest request, CancellationToken ct = default);
    Task<TeamDto> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
