namespace Myrati.Application.Abstractions;

public sealed record ContactLeadEmailMessage(
    string LeadId,
    string Name,
    string Email,
    string Company,
    string Subject,
    string Message,
    DateTimeOffset CreatedAt);
