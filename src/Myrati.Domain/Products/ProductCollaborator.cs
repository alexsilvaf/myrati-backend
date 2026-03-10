using Myrati.Domain.Identity;

namespace Myrati.Domain.Products;

public sealed class ProductCollaborator
{
    public string ProductId { get; set; } = string.Empty;
    public Product Product { get; set; } = null!;

    public string MemberId { get; set; } = string.Empty;
    public AdminUser Member { get; set; } = null!;

    public DateOnly AddedDate { get; set; }

    public bool TasksView { get; set; }
    public bool TasksCreate { get; set; }
    public bool TasksEdit { get; set; }
    public bool TasksDelete { get; set; }

    public bool SprintsView { get; set; }
    public bool SprintsCreate { get; set; }
    public bool SprintsEdit { get; set; }
    public bool SprintsDelete { get; set; }

    public bool LicensesView { get; set; }
    public bool LicensesCreate { get; set; }
    public bool LicensesEdit { get; set; }
    public bool LicensesDelete { get; set; }

    public bool ProductView { get; set; }
    public bool ProductCreate { get; set; }
    public bool ProductEdit { get; set; }
    public bool ProductDelete { get; set; }
}
