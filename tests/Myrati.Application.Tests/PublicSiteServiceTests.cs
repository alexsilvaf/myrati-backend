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
        var emailSender = new TestContactLeadEmailSender();
        var service = new PublicSiteService(
            scope.Context,
            new ContactRequestValidator(),
            publisher,
            new TestBackofficeNotificationPublisher(),
            emailSender,
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
        Assert.Contains(emailSender.Messages, x => x.Email == "fulano@empresa.com");
        Assert.Contains(publisher.Events, x => x.EventType == "contact.received");
    }
}

file sealed class TestContactLeadEmailSender : IContactLeadEmailSender
{
    private readonly List<ContactLeadEmailMessage> _messages = [];

    public IReadOnlyList<ContactLeadEmailMessage> Messages => _messages;

    public Task SendAsync(ContactLeadEmailMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return Task.CompletedTask;
    }
}
