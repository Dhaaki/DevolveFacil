using DevolveFacill.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevolveFacill.Infrastructure.Persistence.Configurations;

public class CustomerRefreshTokenConfiguration : IEntityTypeConfiguration<CustomerRefreshToken>
{
    public void Configure(EntityTypeBuilder<CustomerRefreshToken> builder)
    {
        builder.ToTable("customer_refresh_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Token).IsUnique();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
