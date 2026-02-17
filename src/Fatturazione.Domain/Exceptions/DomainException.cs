namespace Fatturazione.Domain.Exceptions;

/// <summary>
/// Base class for all domain-level exceptions.
/// All domain exceptions represent expected business errors (Tier 1).
/// These are errors the business logic anticipates and can explain clearly.
/// Maps to HTTP 422 when not caught by a more specific handler.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// The business operation that was being attempted (e.g., "IssueInvoice", "CreateCreditNote").
    /// </summary>
    public string? Operation { get; init; }

    /// <summary>
    /// The entity involved in the failed operation (e.g., "Invoice", "Client").
    /// </summary>
    public string? Entity { get; init; }

    /// <summary>
    /// A structured reason explaining why the operation failed.
    /// </summary>
    public string? Reason { get; init; }

    protected DomainException(string message)
        : base(message) { }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
