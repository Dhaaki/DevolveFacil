using DevolveFacill.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevolveFacill.Infrastructure.Persistence.Configurations;

public class ReturnEventConfiguration : IEntityTypeConfiguration<ReturnEvent>
{
    public void Configure(EntityTypeBuilder<ReturnEvent> builder)
    {
        builder.ToTable("return_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Actor).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.HasIndex(x => x.ReturnRequestId);
        builder.HasIndex(x => x.OccurredAt);
    }
}
