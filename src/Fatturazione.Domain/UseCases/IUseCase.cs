namespace Fatturazione.Domain.UseCases;

/// <summary>
/// Represents the actor performing a business operation.
/// Included in all use case requests to provide identity and authorization context.
/// </summary>
/// <param name="UserId">Unique identifier of the authenticated user.</param>
/// <param name="UserName">Display name of the authenticated user.</param>
/// <param name="Roles">Roles assigned to the user for authorization checks.</param>
public record ActorContext(
    Guid UserId,
    string UserName,
    IReadOnlyList<string> Roles)
{
    /// <summary>
    /// Creates a system-level actor for automated/background operations.
    /// </summary>
    public static ActorContext System => new(
        Guid.Empty,
        "System",
        Array.Empty<string>());
}

/// <summary>
/// Defines a single business operation following SBA (Story-Based Architecture).
/// Each use case encapsulates one complete business story with Load, Validate, Execute phases.
/// </summary>
/// <typeparam name="TRequest">The input required to execute the operation.</typeparam>
/// <typeparam name="TResponse">The output produced by the operation.</typeparam>
public interface IUseCase<in TRequest, TResponse>
{
    /// <summary>
    /// Executes the business operation: loads data, validates rules, performs mutations.
    /// </summary>
    /// <param name="request">The request containing all input for the operation.</param>
    /// <returns>The response containing the result of the operation.</returns>
    /// <exception cref="Exceptions.NotFoundException">When a required entity is not found.</exception>
    /// <exception cref="Exceptions.InvalidInputException">When input data fails validation.</exception>
    /// <exception cref="Exceptions.ForbiddenOperationException">When a business rule prevents the operation.</exception>
    /// <exception cref="Exceptions.ConflictException">When the operation conflicts with existing state.</exception>
    Task<TResponse> Execute(TRequest request);
}
