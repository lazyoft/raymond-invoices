namespace Fatturazione.Domain.Exceptions;

/// <summary>
/// Thrown when a business rule prevents the requested operation.
/// Examples: invalid state transition, immutability violation, unauthorized action.
/// Maps to HTTP 403.
/// </summary>
public class ForbiddenOperationException : DomainException
{
    public ForbiddenOperationException(string message)
        : base(message)
    {
        Reason = message;
    }

    public ForbiddenOperationException(string operation, string entity, string reason)
        : base($"Operazione '{operation}' non consentita su {entity}: {reason}")
    {
        Operation = operation;
        Entity = entity;
        Reason = reason;
    }
}
