using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevolveFacill.Infrastructure.Persistence.Configurations;

public class QualityAssessmentConfiguration : IEntityTypeConfiguration<QualityAssessment>
{
    public void Configure(EntityTypeBuilder<QualityAssessment> builder)
    {
        builder.ToTable("quality_assessments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AssessedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Result)
            .HasConversion(v => v.ToString(), v => Enum.Parse<QualityResult>(v))
            .HasMaxLength(20).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.DamageImageKeys)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
    }
}
