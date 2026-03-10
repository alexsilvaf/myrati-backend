using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Myrati.API.Middleware;
using Myrati.API.Tests.Support;
using Xunit;

namespace Myrati.API.Tests;

public sealed class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithForeignKeyDbUpdateException_ReturnsConflictWithFriendlyMessage()
    {
        var middleware = new ApiExceptionMiddleware(
            _ => throw new DbUpdateException("Delete failed.", new InvalidOperationException("FOREIGN KEY constraint failed")),
            NullLogger<ApiExceptionMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(context.Response.Body);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Conflict", payload.Title);
        Assert.Equal("Não é possível concluir a operação porque existem registros vinculados a este item.", payload.Detail);
    }
}
