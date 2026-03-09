using Myrati.Domain.Common;

namespace Myrati.Domain.Settings;

public sealed class ApiKeyCredential : Entity
{
    public string Label { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateOnly CreatedAt { get; set; }
}
