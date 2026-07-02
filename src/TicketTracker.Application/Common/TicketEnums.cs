using TicketTracker.Domain.Enums;

namespace TicketTracker.Application.Common;

/// <summary>
/// Maps ticket enums to/from their canonical API string values
/// (<c>bug|feature|fix</c> and <c>new|ready_for_implementation|in_progress|
/// ready_for_acceptance|done</c>). Parsing is case-insensitive and strict.
/// </summary>
public static class TicketEnums
{
    private static readonly Dictionary<TicketType, string> TypeToApi = new()
    {
        [TicketType.Bug] = "bug",
        [TicketType.Feature] = "feature",
        [TicketType.Fix] = "fix",
    };

    private static readonly Dictionary<TicketState, string> StateToApi = new()
    {
        [TicketState.New] = "new",
        [TicketState.ReadyForImplementation] = "ready_for_implementation",
        [TicketState.InProgress] = "in_progress",
        [TicketState.ReadyForAcceptance] = "ready_for_acceptance",
        [TicketState.Done] = "done",
    };

    private static readonly Dictionary<string, TicketType> ApiToType =
        TypeToApi.ToDictionary(kv => kv.Value, kv => kv.Key);

    private static readonly Dictionary<string, TicketState> ApiToState =
        StateToApi.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static string AllowedTypes => string.Join(", ", TypeToApi.Values);
    public static string AllowedStates => string.Join(", ", StateToApi.Values);

    public static string ToApiValue(this TicketType type) => TypeToApi[type];
    public static string ToApiValue(this TicketState state) => StateToApi[state];

    public static bool TryParseType(string? value, out TicketType type) =>
        ApiToType.TryGetValue((value ?? string.Empty).Trim().ToLowerInvariant(), out type);

    public static bool TryParseState(string? value, out TicketState state) =>
        ApiToState.TryGetValue((value ?? string.Empty).Trim().ToLowerInvariant(), out state);
}
