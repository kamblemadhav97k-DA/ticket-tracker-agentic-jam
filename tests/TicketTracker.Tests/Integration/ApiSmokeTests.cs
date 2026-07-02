using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TicketTracker.Application.Common.Interfaces;
using TicketTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace TicketTracker.Tests.Integration;

public class ApiSmokeTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Email = "qa@example.com";
    private const string Password = "P@ssw0rd123";

    [Fact]
    public async Task Health_IsPublic()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BusinessEndpoints_RequireAuthentication()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/teams");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FullFlow_Register_Verify_Login_And_Crud()
    {
        var client = factory.CreateClient();

        // Register.
        var register = await client.PostAsJsonAsync("/api/auth/register", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        // Login before verification is rejected.
        var earlyLogin = await client.PostAsJsonAsync("/api/auth/login", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.Forbidden, earlyLogin.StatusCode);

        // Issue a verification token out-of-band (email is stubbed) and verify.
        string rawToken;
        using (var scope = factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(Email);
            var verification = sp.GetRequiredService<IEmailVerificationService>();
            rawToken = await verification.IssueTokenAsync(user!.Id);
        }

        var verify = await client.PostAsJsonAsync("/api/auth/verify-email", new { token = rawToken });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        // Login succeeds and returns a JWT.
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<LoginBody>();
        Assert.False(string.IsNullOrWhiteSpace(auth!.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Create a team.
        var teamResp = await client.PostAsJsonAsync("/api/teams", new { name = "Payments Team" });
        Assert.Equal(HttpStatusCode.Created, teamResp.StatusCode);
        var team = await teamResp.Content.ReadFromJsonAsync<IdName>();

        // Duplicate team name conflicts (409).
        var dup = await client.PostAsJsonAsync("/api/teams", new { name = "payments team" });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        // Create an epic.
        var epicResp = await client.PostAsJsonAsync("/api/epics", new { teamId = team!.Id, title = "Checkout" });
        Assert.Equal(HttpStatusCode.Created, epicResp.StatusCode);
        var epic = await epicResp.Content.ReadFromJsonAsync<IdName>();

        // Create a ticket referencing the epic.
        var ticketResp = await client.PostAsJsonAsync("/api/tickets", new
        {
            teamId = team.Id,
            epicId = epic!.Id,
            type = "bug",
            title = "Payment fails for expired card",
            body = "Steps to reproduce...",
        });
        Assert.Equal(HttpStatusCode.Created, ticketResp.StatusCode);
        var ticket = await ticketResp.Content.ReadFromJsonAsync<TicketBody>();
        Assert.Equal("new", ticket!.State);

        // Move it across the board (drag-and-drop persistence).
        var move = await client.PatchAsJsonAsync($"/api/tickets/{ticket.Id}/state", new { state = "in_progress" });
        Assert.Equal(HttpStatusCode.OK, move.StatusCode);

        // Add a comment.
        var comment = await client.PostAsJsonAsync($"/api/tickets/{ticket.Id}/comments", new { body = "Looking into it" });
        Assert.Equal(HttpStatusCode.Created, comment.StatusCode);

        // Board query returns the ticket.
        var board = await client.GetFromJsonAsync<List<TicketBody>>($"/api/tickets?teamId={team.Id}");
        Assert.Single(board!);
        Assert.Equal("in_progress", board![0].State);

        // Deleting a referenced epic conflicts (409).
        var epicDelete = await client.DeleteAsync($"/api/epics/{epic.Id}");
        Assert.Equal(HttpStatusCode.Conflict, epicDelete.StatusCode);

        // Deleting a team with tickets conflicts (409).
        var teamDelete = await client.DeleteAsync($"/api/teams/{team.Id}");
        Assert.Equal(HttpStatusCode.Conflict, teamDelete.StatusCode);

        // Delete the ticket (cascades comments), then the now-empty resources.
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/tickets/{ticket.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/epics/{epic.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/teams/{team.Id}")).StatusCode);
    }

    private sealed record LoginBody(string AccessToken, DateTime ExpiresAtUtc, string Email);
    private sealed record IdName(Guid Id, string Name);
    private sealed record TicketBody(Guid Id, string Type, string State, string Title);
}
