namespace Fatturazione.Domain.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist.
/// Maps to HTTP 404.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message)
        : base(message) { }

    public NotFoundException(string entityName, Guid id)
        : base($"{entityName} con ID {id} non trovato.")
    {
        Entity = entityName;
        Reason = $"ID {id} non trovato";
    }

    public NotFoundException(string entityName, string identifier)
        : base($"{entityName} '{identifier}' non trovato.")
    {
        Entity = entityName;
        Reason = $"'{identifier}' non trovato";
    }
}
