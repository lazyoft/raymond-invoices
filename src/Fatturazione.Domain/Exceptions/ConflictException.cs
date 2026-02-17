namespace Fatturazione.Domain.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with existing state.
/// Examples: duplicate Partita IVA, concurrent invoice number assignment.
/// Maps to HTTP 409.
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message)
    {
        Reason = message;
    }

    public ConflictException(string entity, string conflictDetail)
        : base($"Conflitto su {entity}: {conflictDetail}")
    {
        Entity = entity;
        Reason = conflictDetail;
    }
}
