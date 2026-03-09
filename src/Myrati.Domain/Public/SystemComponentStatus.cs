using Myrati.Domain.Common;

namespace Myrati.Domain.Public;

public sealed class SystemComponentStatus : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "operational";
    public string Uptime { get; set; } = string.Empty;
    public string ResponseTime { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
