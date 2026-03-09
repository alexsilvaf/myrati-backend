using Myrati.Domain.Common;

namespace Myrati.Domain.Public;

public sealed class UptimeSample : Entity
{
    public string Day { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public int SortOrder { get; set; }
}
