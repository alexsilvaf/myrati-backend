using Myrati.Domain.Common;

namespace Myrati.Domain.Costs;

public sealed class CashTransaction : Entity
{
    public string Type { get; set; } = "withdrawal";
    public string Category { get; set; } = "other";
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReferenceProductId { get; set; } = string.Empty;
    public string ReferenceProductName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
