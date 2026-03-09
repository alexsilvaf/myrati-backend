using Myrati.Domain.Common;
using Myrati.Domain.Products;

namespace Myrati.Domain.Clients;

public sealed class Client : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "CNPJ";
    public string Company { get; set; } = string.Empty;
    public DateOnly JoinedDate { get; set; }
    public string Status { get; set; } = "Ativo";

    public ICollection<License> Licenses { get; set; } = new List<License>();
    public ICollection<ConnectedUser> Users { get; set; } = new List<ConnectedUser>();
}
