using System.Text.Json.Serialization;

namespace Myrati.API.Tests.Support;

public sealed record ApiErrorResponse(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("detail")] string Detail);
