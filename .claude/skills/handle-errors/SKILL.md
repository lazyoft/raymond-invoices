---
name: handle-errors
description: Implements two-tier error handling for the Fatturazione invoicing system following SBA patterns. Use when adding error handling to use cases, defining new domain exceptions, or setting up structured logging with actor context. Covers expected errors (validation, authorization) vs unexpected errors (infrastructure), logging levels, and exception-to-HTTP-status mapping.
license: MIT
metadata:
  author: Fabrizio Chignoli
  version: 1.0.0
  category: architecture
  mcp-server: none
---

# Skill: Handle Errors (SBA Two-Tier Pattern)

You are implementing error handling for the Fatturazione invoicing system using the SBA two-tier model: **expected errors** (domain) vs **unexpected errors** (infrastructure).

## Philosophy

Errors are not exceptional surprises -- they are part of the business story. When an actor tries to issue an already-issued invoice, that is an **expected error** and the system should respond clearly and calmly. When the database connection drops, that is an **unexpected error** and the system should alert operators.

The two tiers guide **what to throw**, **how to log**, and **how to respond**.

## Project Context

- **Language:** C# / .NET 8
- **Namespace for exceptions:** `Fatturazione.Domain.Exceptions`
- **Location:** `src/Fatturazione.Domain/Exceptions/`
- **Logging:** `Microsoft.Extensions.Logging.ILogger<T>`
- **Existing patterns:** Validation returns `(bool IsValid, List<string> Errors)` tuples in current Validators

## The Two Tiers

### Tier 1: Expected Errors (Domain)

These are errors the business logic **anticipates**. An actor did something the rules do not allow, or the requested data does not exist. The system knows exactly what happened and can explain it clearly.

| Exception | When to Throw | HTTP Status |
|-----------|--------------|-------------|
| `NotFoundException` | Entity not found by ID | 404 |
| `InvalidInputException` | Validation failures (bad data, missing fields) | 400 |
| `ForbiddenOperationException` | Business rule violation (wrong state, unauthorized action) | 403 / 400 |
| `ConflictException` | Duplicate resource, concurrent modification | 409 |

**Logging level:** `Information` -- these are normal business events, not system failures.

### Tier 2: Unexpected Errors (Infrastructure)

These are errors the business logic **does not anticipate**. The database is down, a network call failed, a null reference occurred. Something is broken.

**Logging level:** `Error` with the full exception object.

These are NOT caught inside use cases. They bubble up to global middleware or the endpoint layer.

## Instructions

### Step 1: Create Domain Exception Classes

Create these files in `src/Fatturazione.Domain/Exceptions/`:

#### NotFoundException.cs

```csharp
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
        : base($"{entityName} con ID {id} non trovato.") { }

    public NotFoundException(string entityName, string identifier)
        : base($"{entityName} '{identifier}' non trovato.") { }
}
```

#### InvalidInputException.cs

```csharp
namespace Fatturazione.Domain.Exceptions;

/// <summary>
/// Thrown when input data fails validation.
/// Carries a list of specific validation errors.
/// Maps to HTTP 400 with ValidationProblemDetails.
/// </summary>
public class InvalidInputException : DomainException
{
    public List<string> Errors { get; }

    public InvalidInputException(List<string> errors)
        : base(FormatErrors(errors))
    {
        Errors = errors;
    }

    public InvalidInputException(string error)
        : base(error)
    {
        Errors = new List<string> { error };
    }

    private static string FormatErrors(List<string> errors)
        => string.Join("; ", errors);
}
```

#### ForbiddenOperationException.cs

```csharp
namespace Fatturazione.Domain.Exceptions;

/// <summary>
/// Thrown when a business rule prevents the requested operation.
/// Examples: invalid state transition, immutability violation, unauthorized action.
/// Maps to HTTP 400 (business rule) or 403 (authorization).
/// </summary>
public class ForbiddenOperationException : DomainException
{
    public ForbiddenOperationException(string message)
        : base(message) { }
}
```

#### ConflictException.cs

```csharp
namespace Fatturazione.Domain.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with existing state.
/// Examples: duplicate Partita IVA, concurrent invoice number assignment.
/// Maps to HTTP 409.
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message) { }
}
```

#### DomainException.cs (Base Class)

```csharp
namespace Fatturazione.Domain.Exceptions;

/// <summary>
/// Base class for all domain-level exceptions.
/// All domain exceptions represent expected business errors (Tier 1).
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message) { }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

### Step 2: Apply Logging Rules

#### Expected Errors -- Log at Information Level

Inside use cases, log expected errors BEFORE throwing them. Always include the actor context using structured logging:

```csharp
// Validation failure (expected: actor submitted bad data)
_logger.LogInformation(
    "Actor {ActorId} submitted invalid invoice {InvoiceId}: {Errors}",
    request.ActorId,
    request.InvoiceId,
    string.Join("; ", errors));

throw new InvalidInputException(errors);
```

```csharp
// State violation (expected: actor tried something not allowed)
_logger.LogInformation(
    "Actor {ActorId} attempted forbidden transition {From} -> {To} on invoice {InvoiceId}",
    request.ActorId,
    invoice.Status,
    InvoiceStatus.Issued,
    invoice.Id);

throw new ForbiddenOperationException(
    $"Impossibile emettere la fattura: transizione da {invoice.Status} a Issued non consentita.");
```

```csharp
// Not found (expected: actor referenced something that doesn't exist)
_logger.LogInformation(
    "Actor {ActorId} requested non-existent invoice {InvoiceId}",
    request.ActorId,
    request.InvoiceId);

throw new NotFoundException("Fattura", request.InvoiceId);
```

#### Unexpected Errors -- Log at Error Level

Unexpected errors are NOT caught inside use cases. They are caught at the endpoint layer or global middleware:

```csharp
// In endpoint or middleware:
catch (Exception ex) when (ex is not DomainException)
{
    logger.LogError(ex,
        "Unexpected error processing request for invoice {InvoiceId}",
        invoiceId);

    return Results.Problem(
        title: "Errore interno del server",
        statusCode: 500);
}
```

#### Successful Operations -- Log at Information Level

Always log the successful completion of a use case:

```csharp
_logger.LogInformation(
    "Invoice {InvoiceId} issued as {InvoiceNumber} by actor {ActorId}",
    invoice.Id,
    invoice.InvoiceNumber,
    request.ActorId);
```

### Step 3: Event Publishing -- Non-Blocking

When a use case publishes domain events (future feature), errors in event publishing must NEVER fail the main operation:

```csharp
// In the Execute phase of a use case:
private async Task<IssueInvoiceResponse> PerformIssuance(
    Invoice invoice, Client client, string? lastNumber, Guid actorId)
{
    // ... main mutation and persistence ...

    // Non-blocking event publishing
    try
    {
        await _eventPublisher.Publish(new InvoiceIssuedEvent(invoice.Id, invoice.InvoiceNumber));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Failed to publish InvoiceIssuedEvent for invoice {InvoiceId}. " +
            "The invoice was issued successfully. Event will be retried.",
            invoice.Id);
        // Do NOT re-throw -- the business operation succeeded
    }

    return new IssueInvoiceResponse(updated!, invoice.InvoiceNumber);
}
```

### Step 4: Map Exceptions to HTTP Responses

In the endpoint layer, catch domain exceptions and map them to appropriate HTTP responses:

```csharp
/// <summary>
/// Standard exception-to-HTTP mapping for use case endpoints.
/// Place in a shared helper or use inline in each endpoint.
/// </summary>
private static async Task<IResult> ExecuteUseCase<TResponse>(
    Func<Task<TResponse>> action,
    ILogger logger)
{
    try
    {
        var result = await action();
        return Results.Ok(result);
    }
    catch (NotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidInputException ex)
    {
        var errorDict = ex.Errors
            .Select((e, i) => new { Key = $"Error{i}", Value = e })
            .ToDictionary(x => x.Key, x => new[] { x.Value });
        return Results.ValidationProblem(errorDict);
    }
    catch (ForbiddenOperationException ex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            { "Operation", new[] { ex.Message } }
        });
    }
    catch (ConflictException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (Exception ex) when (ex is not DomainException)
    {
        logger.LogError(ex, "Unexpected error in use case execution");
        return Results.Problem(
            title: "Errore interno del server",
            statusCode: 500);
    }
}
```

## Logging Rules Summary

| Situation | Level | Include Actor | Include Exception |
|-----------|-------|--------------|-------------------|
| Validation failure | `Information` | Yes | No (message only) |
| Not found | `Information` | Yes | No |
| Forbidden operation | `Information` | Yes | No |
| Conflict | `Information` | Yes | No |
| Successful operation | `Information` | Yes | No |
| Infrastructure failure | `Error` | If available | Yes (full exception) |
| Event publish failure | `Error` | If available | Yes (full exception) |

## Structured Logging Format

Always use structured logging parameters (NOT string interpolation):

```csharp
// CORRECT: structured logging with named parameters
_logger.LogInformation(
    "Actor {ActorId} issued invoice {InvoiceId} as {InvoiceNumber}",
    actorId, invoiceId, invoiceNumber);

// WRONG: string interpolation loses structured data
_logger.LogInformation(
    $"Actor {actorId} issued invoice {invoiceId} as {invoiceNumber}");
```

## Anti-Patterns to Avoid

1. **Never catch and swallow domain exceptions inside a use case** -- let them propagate to the endpoint layer
2. **Never log at Warning/Error for expected business errors** -- a user submitting bad data is not a warning
3. **Never throw generic `Exception`** -- always use a specific `DomainException` subclass
4. **Never let event publishing failures break the main operation** -- wrap in try-catch
5. **Never use string interpolation in log message templates** -- use structured logging parameters
6. **Never expose stack traces in HTTP responses** -- return user-friendly messages
7. **Never silently ignore unexpected errors** -- always log at `Error` level with the full exception

## Checklist

- [ ] Exception classes created in `src/Fatturazione.Domain/Exceptions/`
- [ ] `DomainException` base class exists
- [ ] All four exception types defined: `NotFoundException`, `InvalidInputException`, `ForbiddenOperationException`, `ConflictException`
- [ ] Use case logs validation failures at `Information` level with actor context
- [ ] Use case logs success at `Information` level
- [ ] Endpoint layer catches `DomainException` subtypes and maps to HTTP status codes
- [ ] Unexpected errors logged at `Error` level with full exception
- [ ] Event publishing wrapped in non-blocking try-catch
- [ ] All logging uses structured parameters (no string interpolation)
