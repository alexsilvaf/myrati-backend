using Myrati.Domain.Common;

namespace Myrati.Domain.Products;

public sealed class ProductTask : Entity
{
    public string ProductId { get; set; } = string.Empty;
    public string SprintId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Column { get; set; } = "backlog";
    public string Priority { get; set; } = "medium";
    public string Assignee { get; set; } = string.Empty;
    public string TagsSerialized { get; set; } = "[]";
    public DateOnly CreatedDate { get; set; }
    public int SortOrder { get; set; }

    public Product? Product { get; set; }
    public ProductSprint? Sprint { get; set; }
}
