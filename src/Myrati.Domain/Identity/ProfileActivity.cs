using Myrati.Domain.Common;

namespace Myrati.Domain.Identity;

public sealed class ProfileActivity : Entity
{
    public string AdminUserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string DateDisplay { get; set; } = string.Empty;

    public AdminUser? AdminUser { get; set; }
}
