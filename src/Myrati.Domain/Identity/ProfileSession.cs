using Myrati.Domain.Common;

namespace Myrati.Domain.Identity;

public sealed class ProfileSession : Entity
{
    public string AdminUserId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string LastActiveDisplay { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }

    public AdminUser? AdminUser { get; set; }
}
