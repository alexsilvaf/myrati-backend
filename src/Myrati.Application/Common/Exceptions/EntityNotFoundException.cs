namespace Myrati.Application.Common.Exceptions;

public sealed class EntityNotFoundException(string entityName, string entityId)
    : Exception($"{entityName} '{entityId}' não encontrado.");
