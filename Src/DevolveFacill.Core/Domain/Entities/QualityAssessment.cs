using DevolveFacill.Core.Domain.Enums;

namespace DevolveFacill.Core.Domain.Entities;

public class QualityAssessment
{
    public Guid Id { get; set; }
    public Guid ReturnRequestId { get; set; }
    public ReturnRequest ReturnRequest { get; set; } = null!;
    public string AssessedBy { get; set; } = string.Empty;
    public QualityResult Result { get; set; }
    public string? Notes { get; set; }
    public List<string> DamageImageKeys { get; set; } = [];
    public DateTimeOffset AssessedAt { get; set; }
}
