using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class SettingsAndProfileEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task SettingsAndProfileMutations_ReturnExpectedPayloads()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var updateSettingsResponse = await client.PutAsJsonAsync(
            "/api/v1/backoffice/settings",
            new UpdateSettingsRequest(
                new CompanyInfoDto(
                    $"Myrati Tecnologia {suffix}",
                    "12.345.678/0001-90",
                    $"contato+{suffix}@myrati.com",
                    "(11) 95555-0000",
                    "Rua dos Testes, 100",
                    "Sao Paulo / SP"),
                new RegionalPreferencesDto("pt-BR", "America/Sao_Paulo"),
                new NotificationPreferencesDto(true, true, true, false, true),
                new SecurityPreferencesDto(true, "45")));
        updateSettingsResponse.EnsureSuccessStatusCode();

        var updatedSettings = await updateSettingsResponse.Content.ReadFromJsonAsync<SettingsSnapshotDto>();
        Assert.NotNull(updatedSettings);
        Assert.Equal($"Myrati Tecnologia {suffix}", updatedSettings.CompanyInfo.Name);
        Assert.True(updatedSettings.Security.TwoFactorAuth);

        var createApiKeyResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/settings/api-keys",
            new CreateApiKeyRequest($"Integracao {suffix}", "staging"));
        createApiKeyResponse.EnsureSuccessStatusCode();

        var createdApiKey = await createApiKeyResponse.Content.ReadFromJsonAsync<ApiKeyDto>();
        Assert.NotNull(createdApiKey);
        Assert.Contains("myra_stg_", createdApiKey.Key, StringComparison.Ordinal);

        var rotateApiKeyResponse = await client.PostAsync($"/api/v1/backoffice/settings/api-keys/{createdApiKey.Id}/rotate", null);
        rotateApiKeyResponse.EnsureSuccessStatusCode();

        var rotatedApiKey = await rotateApiKeyResponse.Content.ReadFromJsonAsync<ApiKeyDto>();
        Assert.NotNull(rotatedApiKey);
        Assert.NotEqual(createdApiKey.Key, rotatedApiKey.Key);

        var toggleApiKeyResponse = await client.PostAsync($"/api/v1/backoffice/settings/api-keys/{createdApiKey.Id}/toggle", null);
        toggleApiKeyResponse.EnsureSuccessStatusCode();

        var toggledApiKey = await toggleApiKeyResponse.Content.ReadFromJsonAsync<ApiKeyDto>();
        Assert.NotNull(toggledApiKey);
        Assert.False(toggledApiKey.Active);

        var deleteApiKeyResponse = await client.DeleteAsync($"/api/v1/backoffice/settings/api-keys/{createdApiKey.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteApiKeyResponse.StatusCode);

        var createTeamMemberResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/settings/team-members",
            new CreateTeamMemberRequest(
                $"Membro {suffix}",
                $"membro-{suffix}@myrati.com",
                "Desenvolvedor"));
        createTeamMemberResponse.EnsureSuccessStatusCode();

        var createdTeamMember = await createTeamMemberResponse.Content.ReadFromJsonAsync<TeamMemberDto>();
        Assert.NotNull(createdTeamMember);
        Assert.Equal("Desenvolvedor", createdTeamMember.Role);

        var updateTeamMemberResponse = await client.PutAsJsonAsync(
            $"/api/v1/backoffice/settings/team-members/{createdTeamMember.Id}",
            new UpdateTeamMemberRequest(
                $"Membro Editado {suffix}",
                $"membro-editado-{suffix}@myrati.com",
                "Admin",
                "Ativo"));
        updateTeamMemberResponse.EnsureSuccessStatusCode();

        var updatedTeamMember = await updateTeamMemberResponse.Content.ReadFromJsonAsync<TeamMemberDto>();
        Assert.NotNull(updatedTeamMember);
        Assert.Equal("Admin", updatedTeamMember.Role);
        Assert.Equal("Ativo", updatedTeamMember.Status);

        var deleteTeamMemberResponse = await client.DeleteAsync($"/api/v1/backoffice/settings/team-members/{createdTeamMember.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteTeamMemberResponse.StatusCode);

        var profileResponse = await client.GetAsync("/api/v1/backoffice/profile");
        profileResponse.EnsureSuccessStatusCode();
        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileSnapshotDto>();
        Assert.NotNull(profile);

        var updateProfileResponse = await client.PutAsJsonAsync(
            "/api/v1/backoffice/profile",
            new UpdateProfileRequest(
                $"Admin Master {suffix}",
                "admin@myrati.com",
                "(11) 97777-4444",
                "Plataforma",
                "Sao Paulo / SP"));
        updateProfileResponse.EnsureSuccessStatusCode();

        var updatedProfile = await updateProfileResponse.Content.ReadFromJsonAsync<ProfileInfoDto>();
        Assert.NotNull(updatedProfile);
        Assert.Equal($"Admin Master {suffix}", updatedProfile.Name);

        var sessionToRevoke = profile.ActiveSessions.FirstOrDefault(x => !x.Current);
        Assert.NotNull(sessionToRevoke);

        var revokeSessionResponse = await client.PostAsync(
            $"/api/v1/backoffice/profile/sessions/{sessionToRevoke.Id}/revoke",
            null);
        Assert.Equal(HttpStatusCode.NoContent, revokeSessionResponse.StatusCode);

        var changePasswordResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/profile/change-password",
            new ChangePasswordRequest("Myrati@123", "Myrati@456", "Myrati@456"));
        Assert.Equal(HttpStatusCode.NoContent, changePasswordResponse.StatusCode);

        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var loginWithNewPasswordResponse = await loginClient.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("admin@myrati.com", "Myrati@456"));
        loginWithNewPasswordResponse.EnsureSuccessStatusCode();

        var loginWithNewPassword = await loginWithNewPasswordResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(loginWithNewPassword);
        loginClient.UseBearerToken(loginWithNewPassword.AccessToken);

        var restorePasswordResponse = await loginClient.PostAsJsonAsync(
            "/api/v1/backoffice/profile/change-password",
            new ChangePasswordRequest("Myrati@456", "Myrati@123", "Myrati@123"));
        Assert.Equal(HttpStatusCode.NoContent, restorePasswordResponse.StatusCode);
    }

    [Fact]
    public async Task TeamMemberInvitationFlow_AllowsPasswordSetupAndLogin()
    {
        factory.PasswordSetupEmailSender.Reset();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var invitedEmail = $"convite-{suffix}@myrati.com";
        const string invitedPassword = "Convite@123";

        using var adminClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var adminAuth = await adminClient.LoginAsAdminAsync();
        adminClient.UseBearerToken(adminAuth.AccessToken);

        var createTeamMemberResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/backoffice/settings/team-members",
            new CreateTeamMemberRequest(
                $"Convidado {suffix}",
                invitedEmail,
                "Vendedor"));
        createTeamMemberResponse.EnsureSuccessStatusCode();

        var invitation = factory.PasswordSetupEmailSender.FindByEmail(invitedEmail);
        Assert.NotNull(invitation);

        using var publicClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var passwordSetupSessionResponse = await publicClient.GetAsync(
            $"/api/v1/auth/password-setup?token={Uri.EscapeDataString(invitation.Token)}");
        passwordSetupSessionResponse.EnsureSuccessStatusCode();

        var passwordSetupSession = await passwordSetupSessionResponse.Content.ReadFromJsonAsync<PasswordSetupSessionDto>();
        Assert.NotNull(passwordSetupSession);
        Assert.Equal(invitedEmail, passwordSetupSession.Email);

        var completePasswordSetupResponse = await publicClient.PostAsJsonAsync(
            "/api/v1/auth/password-setup",
            new PasswordSetupRequest(invitation.Token, invitedPassword, invitedPassword));
        Assert.Equal(HttpStatusCode.NoContent, completePasswordSetupResponse.StatusCode);

        var loginResponse = await publicClient.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(invitedEmail, invitedPassword));
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal(invitedEmail, auth.User.Email);
    }
}
