using Myrati.Domain.Common;

namespace Myrati.Domain.Costs;

public sealed class CompanyCost : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Recurrence { get; set; } = "monthly";
    public string Vendor { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? NextBillingDate { get; set; }
    public string Status { get; set; } = "Ativo";
}
