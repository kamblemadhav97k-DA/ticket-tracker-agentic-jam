namespace TicketTracker.Application.Users;

/// <summary>A registered user that a work item can be assigned to.</summary>
public record UserDto(Guid Id, string? Email);

public interface IUserDirectory
{
    /// <summary>All registered users, ordered by email, for assignee pickers.</summary>
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default);
}
