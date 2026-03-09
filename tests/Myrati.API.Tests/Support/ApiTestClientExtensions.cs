using System.Net.Http.Headers;
using System.Net.Http.Json;
using Myrati.Application.Contracts;

namespace Myrati.API.Tests.Support;

public static class ApiTestClientExtensions
{
    public static async Task<AuthResponse> LoginAsAdminAsync(this HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("admin@myrati.com", "Myrati@123"));

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Resposta de autenticação inválida.");
    }

    public static void UseBearerToken(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
