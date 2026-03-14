using Myrati.Domain.Common;

namespace Myrati.Domain.Products;

public sealed class ProductExpense : Entity
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Recurrence { get; set; } = "monthly";
    public string? Notes { get; set; }
    public DateOnly CreatedDate { get; set; }

    public Product? Product { get; set; }
}
