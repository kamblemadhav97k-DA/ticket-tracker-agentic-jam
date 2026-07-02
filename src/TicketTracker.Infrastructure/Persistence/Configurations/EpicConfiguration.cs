using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketTracker.Domain.Entities;

namespace TicketTracker.Infrastructure.Persistence.Configurations;

public class EpicConfiguration : IEntityTypeConfiguration<Epic>
{
    public void Configure(EntityTypeBuilder<Epic> builder)
    {
        builder.ToTable("Epics");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Description);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.ModifiedAt).IsRequired();

        builder.HasIndex(e => e.TeamId);

        // An epic cannot be deleted while tickets reference it.
        builder.HasMany(e => e.Tickets)
            .WithOne(t => t.Epic)
            .HasForeignKey(t => t.EpicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
