using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Contracts;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Application.Validation;
using Myrati.Domain.Identity;
using Myrati.Infrastructure.Persistence;
using Myrati.Infrastructure.Security;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUser()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MyratiDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new MyratiDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var passwordHasher = new PasswordHasher();
        context.AdminUsersSet.Add(new AdminUser
        {
            Id = "TM-001",
            Name = "Admin Master",
            Email = "admin@myrati.com",
            Phone = "(11) 99876-5432",
            Role = "Super Admin",
            Status = "Ativo",
            Department = "Tecnologia",
            Location = "São Paulo, SP",
            PasswordHash = passwordHasher.Hash("Myrati@123"),
            IsPrimaryAccount = true
        });

        await context.SaveChangesAsync();

        IValidator<LoginRequest> validator = new LoginRequestValidator();
        var service = new AuthService(
            context,
            passwordHasher,
            new StubJwtTokenService(),
            validator,
            new PasswordSetupRequestValidator(),
            new TestRealtimeEventPublisher(),
            new TestBackofficeNotificationPublisher());

        var response = await service.LoginAsync(new LoginRequest("admin@myrati.com", "Myrati@123"));

        Assert.Equal("stub-token", response.AccessToken);
        Assert.Equal("TM-001", response.User.Id);
        Assert.Equal("Admin Master", response.User.Name);
        Assert.Equal("Super Admin", response.User.Role);
    }

    private sealed class StubJwtTokenService : IJwtTokenService
    {
        public AccessTokenResult GenerateAccessToken(AdminUser user) =>
            new("stub-token", new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero));
    }
}
