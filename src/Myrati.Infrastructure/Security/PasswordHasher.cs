using Myrati.Application.Abstractions;

namespace Myrati.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string value) => BCrypt.Net.BCrypt.HashPassword(value);

    public bool Verify(string value, string hash) => BCrypt.Net.BCrypt.Verify(value, hash);
}
