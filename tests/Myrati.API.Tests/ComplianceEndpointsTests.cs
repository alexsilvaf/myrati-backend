using System.Net.Http.Json;
using Myrati.Application.Contracts;
using Myrati.API.Tests.Support;
using Xunit;

namespace Myrati.API.Tests;

public sealed class ComplianceEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CreateDataSubjectRequest_AndReadSnapshot_SucceedForAdmin()
    {
        var client = factory.CreateClient();
        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/compliance/data-subject-requests",
            new
            {
                subjectName = "Titular API",
                subjectEmail = "titular.api@empresa.com",
                subjectDocument = "987.654.321-00",
                requestType = "Portabilidade",
                channel = "Email",
                details = "Solicita portabilidade dos dados para outro fornecedor.",
                identityVerified = true,
                assignedAdminUserId = "TM-001",
                dueAtUtc = DateTimeOffset.UtcNow.AddDays(7)
            });

        createResponse.EnsureSuccessStatusCode();

        var snapshotResponse = await client.GetAsync("/api/v1/backoffice/compliance");
        snapshotResponse.EnsureSuccessStatusCode();

        var payload = await snapshotResponse.Content.ReadFromJsonAsync<ComplianceSnapshotDto>();
        Assert.NotNull(payload);
        Assert.Contains(payload!.DataSubjectRequests, item => item.SubjectEmail == "titular.api@empresa.com");
        Assert.True(payload.Metrics.OpenDataSubjectRequests >= 1);
    }
}
