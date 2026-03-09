using Myrati.Domain.Common;

namespace Myrati.Domain.Products;

public sealed class Product : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "Ativo";
    public DateOnly CreatedDate { get; set; }
    public string Version { get; set; } = string.Empty;

    public ICollection<ProductPlan> Plans { get; set; } = new List<ProductPlan>();
    public ICollection<License> Licenses { get; set; } = new List<License>();
}
