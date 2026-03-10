namespace Myrati.Application.Abstractions;

public interface ICurrentUserContext
{
    string? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
