namespace Fatturazione.Domain.Exceptions;

/// <summary>
/// Thrown when input data fails validation.
/// Carries a list of specific validation errors.
/// Maps to HTTP 400 with ValidationProblemDetails.
/// </summary>
public class InvalidInputException : DomainException
{
    /// <summary>
    /// The list of specific validation errors that caused this exception.
    /// </summary>
    public List<string> Errors { get; }

    public InvalidInputException(List<string> errors)
        : base(FormatErrors(errors))
    {
        Errors = errors;
        Reason = FormatErrors(errors);
    }

    public InvalidInputException(string error)
        : base(error)
    {
        Errors = new List<string> { error };
        Reason = error;
    }

    private static string FormatErrors(List<string> errors)
        => string.Join("; ", errors);
}
