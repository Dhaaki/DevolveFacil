using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevolveFacill.Infrastructure.Persistence.Configurations;

public class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("return_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RequestNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.RequestNumber).IsUnique();
        builder.Property(x => x.ResolutionType)
            .HasConversion(v => v.ToString(), v => Enum.Parse<ResolutionType>(v))
            .HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status)
            .HasConversion(v => v.ToString(), v => Enum.Parse<ReturnStatus>(v))
            .HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReasonCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReasonNotes).HasMaxLength(1000);
        builder.Property(x => x.Platform).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CarrierCode).HasMaxLength(50);
        builder.Property(x => x.VoucherCode).HasMaxLength(100);
        builder.Property(x => x.ErpReturnOrderId).HasMaxLength(100);

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.ReturnRequests)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.ReturnRequest)
            .HasForeignKey(x => x.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Shipment)
            .WithOne(x => x.ReturnRequest)
            .HasForeignKey<Shipment>(x => x.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.QualityAssessment)
            .WithOne(x => x.ReturnRequest)
            .HasForeignKey<QualityAssessment>(x => x.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Events)
            .WithOne(x => x.ReturnRequest)
            .HasForeignKey(x => x.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
