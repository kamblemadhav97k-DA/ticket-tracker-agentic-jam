using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketTracker.Domain.Entities;

namespace TicketTracker.Infrastructure.Persistence.Configurations;

public class TicketLinkConfiguration : IEntityTypeConfiguration<TicketLink>
{
    public void Configure(EntityTypeBuilder<TicketLink> builder)
    {
        builder.ToTable("TicketLinks");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.CreatedAt).IsRequired();

        // Both endpoints reference Tickets. Restrict on delete; the ticket service
        // removes a ticket's links explicitly before deleting it (provider-independent).
        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(l => l.SourceTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(l => l.TargetTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        // One row per ordered pair; the service also blocks the reverse direction.
        builder.HasIndex(l => new { l.SourceTicketId, l.TargetTicketId }).IsUnique();
        builder.HasIndex(l => l.TargetTicketId);
    }
}
