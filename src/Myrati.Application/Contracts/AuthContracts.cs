namespace Myrati.Application.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthUserDto(string Id, string Name, string Email, string Role);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, AuthUserDto User);

public sealed record PasswordSetupSessionDto(string Name, string Email, DateTimeOffset ExpiresAt);

public sealed record PasswordSetupRequest(string Token, string NewPassword, string ConfirmPassword);
