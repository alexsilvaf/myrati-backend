using Myrati.Domain.Common;

namespace Myrati.Domain.Dashboard;

public sealed class ActivityFeedItem : Entity
{
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimeDisplay { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public int SortOrder { get; set; }
}
