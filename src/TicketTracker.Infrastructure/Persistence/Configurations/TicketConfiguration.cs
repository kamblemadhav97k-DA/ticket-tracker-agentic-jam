using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketTracker.Domain.Entities;
using TicketTracker.Infrastructure.Identity;

namespace TicketTracker.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Body).IsRequired();

        // Persist enums as readable strings.
        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.State).HasConversion<string>().HasMaxLength(40).IsRequired();

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.ModifiedAt).IsRequired();
        builder.Property(t => t.CreatedById).IsRequired();

        // Board queries filter by team and order within a state.
        builder.HasIndex(t => new { t.TeamId, t.State });
        builder.HasIndex(t => t.EpicId);
        builder.HasIndex(t => t.ParentId);

        // Self-referencing parent/child hierarchy. Restrict so a parent with children
        // cannot be deleted implicitly; the service reparents children explicitly first.
        builder.HasOne(t => t.Parent)
            .WithMany(t => t.Children)
            .HasForeignKey(t => t.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ticket author (AspNetUsers). No navigation on the domain entity by design.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional assignee (AspNetUsers). No navigation on the domain entity by design.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.AssignedToId);
    }
}
