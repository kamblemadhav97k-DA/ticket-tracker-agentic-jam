using System.ComponentModel.DataAnnotations;

namespace TicketTracker.Application.Epics;

public record EpicDto(
    Guid Id, Guid TeamId, string Title, string? Description, int TicketCount, DateTime CreatedAt, DateTime ModifiedAt);

public record CreateEpicRequest(
    [Required] Guid TeamId,
    [Required] string Title,
    string? Description);

// Team is fixed at creation and cannot be changed, so it is not part of the update payload.
public record UpdateEpicRequest(
    [Required] string Title,
    string? Description);

public interface IEpicService
{
    Task<IReadOnlyList<EpicDto>> GetAllAsync(Guid? teamId, CancellationToken ct = default);
    Task<EpicDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EpicDto> CreateAsync(CreateEpicRequest request, CancellationToken ct = default);
    Task<EpicDto> UpdateAsync(Guid id, UpdateEpicRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
