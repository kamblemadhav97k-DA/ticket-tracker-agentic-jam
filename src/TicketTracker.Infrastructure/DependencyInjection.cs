using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TicketTracker.Application.Comments;
using TicketTracker.Application.Common.Interfaces;
using TicketTracker.Application.Epics;
using TicketTracker.Application.Teams;
using TicketTracker.Application.Tickets;
using TicketTracker.Application.Users;
using TicketTracker.Infrastructure.Identity;
using TicketTracker.Infrastructure.Options;
using TicketTracker.Infrastructure.Persistence;
using TicketTracker.Infrastructure.Services;

namespace TicketTracker.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer: PostgreSQL (EF Core), ASP.NET
/// Core Identity (Argon2id), JWT bearer authentication, SMTP email, and health checks.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---- Options ------------------------------------------------------
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));

        // ---- PostgreSQL via EF Core ---------------------------------------
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // ---- ASP.NET Core Identity (Argon2id password hashing) ------------
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Replace the default PBKDF2 hasher with Argon2id.
        services.AddScoped<IPasswordHasher<ApplicationUser>, Argon2PasswordHasher>();

        // ---- JWT bearer authentication ------------------------------------
        var jwt = configuration.GetSection(JwtOptions.SectionName);
        var secret = jwt["Secret"];
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            string.IsNullOrWhiteSpace(secret)
                                ? new string('0', 32) // placeholder; real secret comes from configuration/env
                                : secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        // ---- Application-facing infrastructure services -------------------
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // ---- Business services (CRUD) -------------------------------------
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IEpicService, EpicService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IUserDirectory, UserDirectory>();

        // ---- Health checks ------------------------------------------------
        services
            .AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(name: "database");

        return services;
    }
}
