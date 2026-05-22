using DevolveFacill.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevolveFacill.Infrastructure.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CarrierCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TrackingCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LabelStorageKey).HasMaxLength(500);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CarrierPayload).HasColumnType("jsonb").IsRequired();
    }
}
