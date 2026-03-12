using Myrati.Domain.Common;

namespace Myrati.Domain.Notifications;

public sealed class AdminNotification : Entity
{
    public string RecipientAdminUserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
