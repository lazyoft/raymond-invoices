---
name: create-use-case
description: Creates SBA (Story-Based Architecture) use cases in C# following the three-phase pattern (Load, Validate, Execute). Use when adding new business operations like creating invoices, issuing credit notes, transitioning statuses, or any domain action that combines data loading, validation, and execution into a single cohesive unit.
license: MIT
metadata:
  author: Fabrizio Chignoli
  version: 1.0.0
  category: architecture
  mcp-server: none
---

# Skill: Create SBA Use Case

You are creating a use case following Story-Based Architecture (SBA) for the Fatturazione invoicing system. Every use case encapsulates a single business story with exactly three phases: Load, Validate, Execute.

## Philosophy

A use case is a **self-contained business story**. It reads like a narrative:

> "To issue an invoice, I need to load the invoice and its client, validate that the invoice can transition to Issued status and has valid data, then assign a number and mark it as issued."

The use case is the **sole public entry point** for a business operation. Endpoints become thin wrappers that delegate to use cases. Services remain stateless calculation/utility helpers.

## Project Context

- **Language:** C# / .NET 8
- **Namespace:** `Fatturazione.Domain.UseCases`
- **Location:** `src/Fatturazione.Domain/UseCases/`
- **Dependency injection:** Constructor injection via ASP.NET Core DI container
- **Existing patterns:** Services in `Fatturazione.Domain.Services`, Repositories in `Fatturazione.Infrastructure.Repositories`
- **Testing:** xUnit + FluentAssertions + NSubstitute

## Instructions

When asked to create a use case, follow this process:

### Step 1: Identify the Business Story

Ask yourself:
1. What is the actor trying to accomplish? (e.g., "issue an invoice", "create a credit note")
2. What data must be loaded from persistence?
3. What business rules must be satisfied?
4. What side effects occur on success? (persist, calculate, notify)

### Step 2: Define Request and Response Records

Place these inside the use case file, above the class definition.

```csharp
// Request: everything the actor provides to trigger the story
public record IssueInvoiceRequest(Guid InvoiceId, Guid ActorId);

// Response: everything the actor needs to know after the story completes
public record IssueInvoiceResponse(Invoice Invoice, string InvoiceNumber);
```

Rules for Request/Response:
- Use C# `record` types (immutable by default)
- Request MUST include an `ActorId` (Guid) to identify who is performing the action
- Request contains only the minimal input needed (IDs, user-provided data)
- Response contains the result plus any computed values the caller needs
- Never expose internal domain state unnecessarily in the response

### Step 3: Implement the Three-Phase Pattern

```csharp
using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using Fatturazione.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Fatturazione.Domain.UseCases;

// Request/Response records
public record IssueInvoiceRequest(Guid InvoiceId, Guid ActorId);
public record IssueInvoiceResponse(Invoice Invoice, string InvoiceNumber);

/// <summary>
/// Issues an invoice: assigns a progressive number and transitions status to Issued.
/// Art. 21 DPR 633/72 - Fatturazione delle operazioni.
/// </summary>
public class IssueInvoice
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IInvoiceNumberingService _numberingService;
    private readonly IInvoiceCalculationService _calculationService;
    private readonly ILogger<IssueInvoice> _logger;

    public IssueInvoice(
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IInvoiceNumberingService numberingService,
        IInvoiceCalculationService calculationService,
        ILogger<IssueInvoice> logger)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _numberingService = numberingService;
        _calculationService = calculationService;
        _logger = logger;
    }

    // ── Sole public method ──────────────────────────────────────────
    public async Task<IssueInvoiceResponse> Execute(IssueInvoiceRequest request)
    {
        // Phase 1: Load
        var (invoice, client, lastNumber) = await GetInvoiceWithDependencies(request.InvoiceId);

        // Phase 2: Validate
        ValidateCanIssue(invoice, request.ActorId);

        // Phase 3: Execute
        var result = await PerformIssuance(invoice, client, lastNumber);

        return result;
    }

    // ── Phase 1: Load ───────────────────────────────────────────────
    // Method name: GetXxx — loads all data needed for the story
    private async Task<(Invoice Invoice, Client Client, string? LastNumber)>
        GetInvoiceWithDependencies(Guid invoiceId)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId)
            ?? throw new NotFoundException($"Fattura con ID {invoiceId} non trovata.");

        var client = await _clientRepository.GetByIdAsync(invoice.ClientId)
            ?? throw new NotFoundException($"Cliente con ID {invoice.ClientId} non trovato.");

        var lastNumber = await _invoiceRepository.GetLastInvoiceNumberAsync();

        return (invoice, client, lastNumber);
    }

    // ── Phase 2: Validate ───────────────────────────────────────────
    // Method name: ValidateXxx — checks all business rules, throws on failure
    private void ValidateCanIssue(Invoice invoice, Guid actorId)
    {
        if (!Validators.InvoiceValidator.CanTransitionTo(invoice.Status, InvoiceStatus.Issued))
        {
            _logger.LogInformation(
                "Actor {ActorId} attempted invalid transition from {CurrentStatus} to Issued for invoice {InvoiceId}",
                actorId, invoice.Status, invoice.Id);

            throw new ForbiddenOperationException(
                $"Impossibile emettere la fattura: transizione da {invoice.Status} a Issued non consentita.");
        }

        var (isValid, errors) = Validators.InvoiceValidator.Validate(invoice);
        if (!isValid)
        {
            _logger.LogInformation(
                "Actor {ActorId} submitted invalid invoice {InvoiceId}: {Errors}",
                actorId, invoice.Id, string.Join("; ", errors));

            throw new InvalidInputException(errors);
        }
    }

    // ── Phase 3: Execute ────────────────────────────────────────────
    // Method name: PerformXxx — mutates state, persists, returns response
    private async Task<IssueInvoiceResponse> PerformIssuance(
        Invoice invoice, Client client, string? lastNumber)
    {
        invoice.Client = client;
        _calculationService.CalculateInvoiceTotals(invoice);

        invoice.InvoiceNumber = _numberingService.GenerateNextInvoiceNumber(lastNumber);
        invoice.Status = InvoiceStatus.Issued;

        var updated = await _invoiceRepository.UpdateAsync(invoice);

        _logger.LogInformation(
            "Invoice {InvoiceId} issued as {InvoiceNumber}",
            invoice.Id, invoice.InvoiceNumber);

        return new IssueInvoiceResponse(updated!, invoice.InvoiceNumber);
    }
}
```

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| File name | PascalCase business intent | `IssueInvoice.cs`, `CreateCreditNote.cs` |
| Class name | Same as file, no suffix | `IssueInvoice`, `CreateCreditNote` |
| Request record | `{UseCaseName}Request` | `IssueInvoiceRequest` |
| Response record | `{UseCaseName}Response` | `IssueInvoiceResponse` |
| Load method(s) | `GetXxx` | `GetInvoiceWithDependencies` |
| Validate method(s) | `ValidateXxx` | `ValidateCanIssue` |
| Execute method(s) | `PerformXxx` | `PerformIssuance` |

## Rules

### Structure Rules
1. **One use case per file** in `src/Fatturazione.Domain/UseCases/`
2. **One public method:** `Execute(TRequest request)` -- everything else is `private`
3. **Three phases, always in order:** Load -> Validate -> Execute
4. **Constructor injection only:** all dependencies via constructor
5. **Request/Response records** defined in the same file, above the class

### Phase Rules

**Phase 1 - Load (GetXxx):**
- Load ALL data the story needs before any validation
- Throw `NotFoundException` if required entities are missing
- Return loaded data via tuple or a private record
- No business logic here -- just fetching

**Phase 2 - Validate (ValidateXxx):**
- Check ALL business rules before mutating anything
- Throw `ForbiddenOperationException` for authorization/state violations
- Throw `InvalidInputException` for data validation failures
- Log validation failures at `Information` level with actor context
- No side effects, no persistence calls

**Phase 3 - Execute (PerformXxx):**
- Mutate domain state, call calculation services
- Persist changes via repositories
- Log the successful outcome at `Information` level
- Return the response record
- Wrap non-critical side effects (events, notifications) in try-catch

### DI Registration

Register use cases as scoped services in `Program.cs`:

```csharp
// Register use cases
builder.Services.AddScoped<IssueInvoice>();
builder.Services.AddScoped<CreateCreditNote>();
```

### Endpoint Integration

Endpoints become thin wrappers that map HTTP to use case execution:

```csharp
private static async Task<IResult> IssueInvoice(
    Guid id,
    UseCases.IssueInvoice useCase)
{
    try
    {
        var request = new IssueInvoiceRequest(id, ActorId: Guid.Empty); // TODO: from auth
        var response = await useCase.Execute(request);
        return Results.Ok(response.Invoice);
    }
    catch (NotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
    catch (ForbiddenOperationException ex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            { "Status", new[] { ex.Message } }
        });
    }
    catch (InvalidInputException ex)
    {
        var errorDict = ex.Errors.Select((e, i) => new { Key = $"Error{i}", Value = e })
            .ToDictionary(x => x.Key, x => new[] { x.Value });
        return Results.ValidationProblem(errorDict);
    }
}
```

## When NOT to Create a Use Case

- **Pure calculations** with no persistence (keep as Services)
- **Simple CRUD** with no business rules (keep in Endpoints directly)
- **Static validation** with no loaded context (keep as Validators)
- **Infrastructure concerns** like XML generation (keep as Services)

A use case is warranted when an operation requires **loading data + validating rules + persisting changes** as a cohesive story.

## Checklist Before Completing

- [ ] File created at `src/Fatturazione.Domain/UseCases/{Name}.cs`
- [ ] Request record includes `ActorId`
- [ ] Response record contains only what the caller needs
- [ ] Three phases clearly separated with `GetXxx`, `ValidateXxx`, `PerformXxx` naming
- [ ] Only `Execute` is public
- [ ] `ILogger<T>` injected and used for validation failures + success
- [ ] Domain exceptions used (`NotFoundException`, `ForbiddenOperationException`, `InvalidInputException`)
- [ ] Registered in `Program.cs` as scoped
- [ ] Tests written (see `write-tests` skill)
