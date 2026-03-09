using Myrati.Domain.Common;

namespace Myrati.Domain.Public;

public sealed class SystemStatusMetadata : Entity
{
    public string LastUpdatedDisplay { get; set; } = string.Empty;
}
