using Myrati.Domain.Common;
using Myrati.Domain.Clients;

namespace Myrati.Domain.Products;

public sealed class License : Entity
{
    public string ClientId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int ActiveUsers { get; set; }
    public string Status { get; set; } = "Ativa";
    public DateOnly StartDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public decimal MonthlyValue { get; set; }
    public decimal? DevelopmentCost { get; set; }
    public decimal? RevenueSharePercent { get; set; }

    public Client? Client { get; set; }
    public Product? Product { get; set; }
}
