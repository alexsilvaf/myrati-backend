using Myrati.Domain.Common;

namespace Myrati.Domain.Products;

public sealed class ProductPlan : Entity
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? MaxUsers { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal? DevelopmentCost { get; set; }
    public decimal? MaintenanceCost { get; set; }
    public decimal? RevenueSharePercent { get; set; }
    public decimal? MaintenanceProfitMargin { get; set; }

    public Product? Product { get; set; }
}
