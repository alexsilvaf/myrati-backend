using Myrati.Domain.Common;

namespace Myrati.Domain.Public;

public sealed class SystemIncident : Entity
{
    public string DateDisplay { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Resolved { get; set; }
    public int SortOrder { get; set; }
}
