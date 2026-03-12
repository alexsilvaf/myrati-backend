using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Myrati.Application.Abstractions;
using Myrati.Application.Contracts;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Application.Validation;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class PublicSiteServiceTests
{
    [Fact]
    public async Task SubmitContactAsync_PersistsLeadAndPublishesRealtimeEvent()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = new PublicSiteService(
            scope.Context,
            new ContactRequestValidator(),
            publisher,
            new TestBackofficeNotificationPublisher(),
            new NoopContactLeadEmailSender(),
            NullLogger<PublicSiteService>.Instance);

        await service.SubmitContactAsync(new ContactRequest(
            "Fulano da Silva",
            "fulano@empresa.com",
            "Empresa XPTO",
            "Interesse comercial",
            "Gostaria de uma demonstração da plataforma."));

        var lead = await scope.Context.ContactLeadsSet.FirstOrDefaultAsync();
        Assert.NotNull(lead);
        Assert.Equal("Fulano da Silva", lead.Name);
        Assert.Contains(publisher.Events, x => x.EventType == "contact.received");
    }
}

file sealed class NoopContactLeadEmailSender : IContactLeadEmailSender
{
    public Task SendAsync(ContactLeadEmailMessage message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
