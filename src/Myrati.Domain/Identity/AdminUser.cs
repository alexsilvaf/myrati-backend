using Myrati.Domain.Common;
using Myrati.Domain.Products;

namespace Myrati.Domain.Identity;

public sealed class AdminUser : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = "Viewer";
    public string Status { get; set; } = "Ativo";
    public string Department { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsPrimaryAccount { get; set; }

    public ICollection<ProfileSession> Sessions { get; set; } = new List<ProfileSession>();
    public ICollection<ProfileActivity> Activities { get; set; } = new List<ProfileActivity>();
    public ICollection<ProductCollaborator> ProductCollaborations { get; set; } = new List<ProductCollaborator>();
}
