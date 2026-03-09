using Myrati.Domain.Common;
using Myrati.Domain.Products;

namespace Myrati.Domain.Clients;

public sealed class ConnectedUser : Entity
{
    public string ClientId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LastActiveDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = "Offline";

    public Client? Client { get; set; }
    public Product? Product { get; set; }
}
