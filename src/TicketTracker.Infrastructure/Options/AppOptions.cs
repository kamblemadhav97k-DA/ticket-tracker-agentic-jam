namespace TicketTracker.Infrastructure.Options;

public class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Public base URL of the SPA, used to build verification links.</summary>
    public string ClientUrl { get; set; } = "http://localhost:5173";
}
