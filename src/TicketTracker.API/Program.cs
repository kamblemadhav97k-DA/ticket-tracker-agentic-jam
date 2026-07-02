using Microsoft.OpenApi;
using TicketTracker.API.Extensions;
using TicketTracker.API.Infrastructure;
using TicketTracker.Application;
using TicketTracker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC controllers --------------------------------------------------------
builder.Services.AddControllers();

// ---- Problem-details error handling -----------------------------------------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---- Application & Infrastructure layers (Clean Architecture) ---------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- CORS for the SPA frontend ----------------------------------------------
const string SpaCorsPolicy = "SpaCorsPolicy";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
    options.AddPolicy(SpaCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ---- Swagger / OpenAPI (with JWT bearer support) ----------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ticket Tracker API",
        Version = "v1",
        Description = "Kanban-style ticket tracker — API (Milestone 1 scaffolding)."
    });

    // Enables the "Authorize" button in Swagger UI for JWT bearer tokens.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT bearer token."
    });
});

var app = builder.Build();

// ---- HTTP request pipeline ---------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket Tracker API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseExceptionHandler();

app.UseCors(SpaCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Public liveness/readiness endpoint.
app.MapHealthChecks("/health");

// Apply EF Core migrations on startup (schema only; no seed data).
await app.MigrateDatabaseAsync();

app.Run();

// Exposed for integration testing in later milestones.
public partial class Program;
