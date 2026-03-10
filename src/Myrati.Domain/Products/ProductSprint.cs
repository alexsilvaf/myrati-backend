using Myrati.Domain.Common;

namespace Myrati.Domain.Products;

public sealed class ProductSprint : Entity
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "Planejada";
    public int SortOrder { get; set; }

    public Product? Product { get; set; }
    public ICollection<ProductTask> Tasks { get; set; } = new List<ProductTask>();
}
