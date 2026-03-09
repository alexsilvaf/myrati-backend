using Myrati.Application.Common.Exceptions;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Application.Validation;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class ClientsServiceTests
{
    [Fact]
    public async Task DeleteClientAsync_WithActiveLicense_ThrowsConflictException()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = new ClientsService(
            scope.Context,
            new CreateClientRequestValidator(),
            new UpdateClientRequestValidator(),
            publisher);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteClientAsync("CLI-001"));
        Assert.Empty(publisher.Events);
    }
}
