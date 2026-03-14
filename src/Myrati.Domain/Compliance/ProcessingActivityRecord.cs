using Myrati.Domain.Common;

namespace Myrati.Domain.Compliance;

public sealed class ProcessingActivityRecord : Entity
{
    public string Name { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
    public string DataSubjectCategories { get; set; } = string.Empty;
    public string PersonalDataCategories { get; set; } = string.Empty;
    public string SharedWith { get; set; } = string.Empty;
    public string RetentionPolicy { get; set; } = string.Empty;
    public string SecurityMeasures { get; set; } = string.Empty;
    public string OwnerArea { get; set; } = string.Empty;
    public bool InternationalTransfer { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? ReviewDueAtUtc { get; set; }
}
