using Myrati.Domain.Common;

namespace Myrati.Domain.Identity;

public sealed class PasswordSetupToken : Entity
{
    public string AdminUserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    public AdminUser? AdminUser { get; set; }
}
