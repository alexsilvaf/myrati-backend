using Myrati.Domain.Common;

namespace Myrati.Domain.Dashboard;

public sealed class RevenueSnapshot : Entity
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Licenses { get; set; }
    public int SortOrder { get; set; }
}
