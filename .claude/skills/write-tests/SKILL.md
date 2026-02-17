---
name: write-tests
description: Writes xUnit tests for SBA use cases in the Fatturazione invoicing system. Use when creating tests for use cases, services, or validators. Follows the three-phase testing pattern (load failures, validation failures, execution success/failure) with NSubstitute mocking and FluentAssertions. Preserves the project's existing test conventions.
license: MIT
metadata:
  author: Fabrizio Chignoli
  version: 1.0.0
  category: testing
  mcp-server: none
---

# Skill: Write Tests for SBA Use Cases

You are writing xUnit tests for the Fatturazione invoicing system, specifically targeting SBA use cases that follow the three-phase pattern (Load, Validate, Execute).

## Philosophy

Tests mirror the three phases of a use case. Each phase has distinct failure modes, and tests should cover all of them. A well-tested use case has:

1. **Load failure tests** -- what happens when entities are not found?
2. **Validation failure tests** -- what happens when business rules are violated?
3. **Execution success tests** -- does the happy path produce correct results?
4. **Execution failure tests** -- do edge cases in the execute phase work correctly?

## Project Context

- **Test framework:** xUnit
- **Assertions:** FluentAssertions (`Should()`, `Be()`, `BeEmpty()`, etc.)
- **Mocking:** NSubstitute (`Substitute.For<T>()`, `Returns()`, `Received()`)
- **Test projects:**
  - `tests/Fatturazione.Domain.Tests/` -- domain services, validators, use cases
  - `tests/Fatturazione.Api.Tests/` -- endpoint integration tests
- **Naming:** Test class = `{SubjectUnderTest}Tests.cs`
- **Pattern:** Arrange-Act-Assert with `// Arrange`, `// Act`, `// Assert` comments
- **Conventions from existing code:**
  - Constructor sets up SUT and mocks (no `[SetUp]` attribute)
  - SUT stored as `_sut`
  - Mocks stored as `_{name}Mock` or `_{name}ServiceMock`
  - `#region` blocks to group related tests
  - `[Fact]` for single-case tests, `[Theory]` with `[InlineData]` for parameterized
  - XML doc comments on helper methods
  - Italian error message assertions where applicable

## Instructions

### Step 1: Create the Test File

For a use case named `IssueInvoice`, create:
`tests/Fatturazione.Domain.Tests/UseCases/IssueInvoiceTests.cs`

### Step 2: Set Up Test Class Structure

```csharp
using Fatturazione.Domain.Exceptions;
using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using Fatturazione.Domain.UseCases;
using Fatturazione.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Fatturazione.Domain.Tests.UseCases;

/// <summary>
/// Tests for IssueInvoice use case.
/// Art. 21 DPR 633/72 - Fatturazione delle operazioni.
/// </summary>
public class IssueInvoiceTests
{
    private readonly IInvoiceRepository _invoiceRepositoryMock;
    private readonly IClientRepository _clientRepositoryMock;
    private readonly IInvoiceNumberingService _numberingServiceMock;
    private readonly IInvoiceCalculationService _calculationServiceMock;
    private readonly ILogger<IssueInvoice> _loggerMock;
    private readonly IssueInvoice _sut;

    public IssueInvoiceTests()
    {
        _invoiceRepositoryMock = Substitute.For<IInvoiceRepository>();
        _clientRepositoryMock = Substitute.For<IClientRepository>();
        _numberingServiceMock = Substitute.For<IInvoiceNumberingService>();
        _calculationServiceMock = Substitute.For<IInvoiceCalculationService>();
        _loggerMock = Substitute.For<ILogger<IssueInvoice>>();

        _sut = new IssueInvoice(
            _invoiceRepositoryMock,
            _clientRepositoryMock,
            _numberingServiceMock,
            _calculationServiceMock,
            _loggerMock);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    #region Helper Methods

    /// <summary>
    /// Creates a valid draft invoice ready for issuance.
    /// </summary>
    private static Invoice CreateDraftInvoice(Guid? id = null, Guid? clientId = null)
    {
        var invoiceId = id ?? Guid.NewGuid();
        var cId = clientId ?? Guid.NewGuid();

        return new Invoice
        {
            Id = invoiceId,
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            ClientId = cId,
            Status = InvoiceStatus.Draft,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Consulenza",
                    Quantity = 1,
                    UnitPrice = 1000m,
                    IvaRate = IvaRate.Standard
                }
            }
        };
    }

    /// <summary>
    /// Creates a standard client for testing.
    /// </summary>
    private static Client CreateClient(Guid? id = null)
    {
        return new Client
        {
            Id = id ?? Guid.NewGuid(),
            RagioneSociale = "Test SRL",
            PartitaIva = "12345678903",
            ClientType = ClientType.Company,
            SubjectToRitenuta = false
        };
    }

    /// <summary>
    /// Creates a standard request for testing.
    /// </summary>
    private static IssueInvoiceRequest CreateRequest(Guid invoiceId)
    {
        return new IssueInvoiceRequest(invoiceId, ActorId: Guid.NewGuid());
    }

    #endregion

    // ── Phase 1: Load Failure Tests ─────────────────────────────────

    #region Load Failures

    [Fact]
    public async Task Execute_InvoiceNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var request = CreateRequest(Guid.NewGuid());
        _invoiceRepositoryMock.GetByIdAsync(request.InvoiceId)
            .Returns((Invoice?)null);

        // Act
        var act = () => _sut.Execute(request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*non trovato*");
    }

    [Fact]
    public async Task Execute_ClientNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var invoice = CreateDraftInvoice();
        var request = CreateRequest(invoice.Id);

        _invoiceRepositoryMock.GetByIdAsync(invoice.Id).Returns(invoice);
        _clientRepositoryMock.GetByIdAsync(invoice.ClientId).Returns((Client?)null);

        // Act
        var act = () => _sut.Execute(request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Cliente*non trovato*");
    }

    #endregion

    // ── Phase 2: Validation Failure Tests ───────────────────────────

    #region Validation Failures

    [Fact]
    public async Task Execute_InvoiceAlreadyIssued_ThrowsForbiddenOperationException()
    {
        // Arrange
        var invoice = CreateDraftInvoice();
        invoice.Status = InvoiceStatus.Issued; // Already issued
        var client = CreateClient(invoice.ClientId);
        var request = CreateRequest(invoice.Id);

        _invoiceRepositoryMock.GetByIdAsync(invoice.Id).Returns(invoice);
        _clientRepositoryMock.GetByIdAsync(invoice.ClientId).Returns(client);
        _invoiceRepositoryMock.GetLastInvoiceNumberAsync().Returns("2026/001");

        // Act
        var act = () => _sut.Execute(request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenOperationException>()
            .WithMessage("*transizione*non consentita*");
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    [InlineData(InvoiceStatus.Overdue)]
    public async Task Execute_InvalidSourceStatus_ThrowsForbiddenOperationException(
        InvoiceStatus invalidStatus)
    {
        // Arrange
        var invoice = CreateDraftInvoice();
        invoice.Status = invalidStatus;
        var client = CreateClient(invoice.ClientId);
        var request = CreateRequest(invoice.Id);

        _invoiceRepositoryMock.GetByIdAsync(invoice.Id).Returns(invoice);
        _clientRepositoryMock.GetByIdAsync(invoice.ClientId).Returns(client);
        _invoiceRepositoryMock.GetLastInvoiceNumberAsync().Returns("2026/001");

        // Act
        var act = () => _sut.Execute(request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    #endregion

    // ── Phase 3: Execution Success Tests ────────────────────────────

    #region Execution Success

    [Fact]
    public async Task Execute_ValidDraftInvoice_ReturnsIssuedInvoice()
    {
        // Arrange
        var invoice = CreateDraftInvoice();
        var client = CreateClient(invoice.ClientId);
        var request = CreateRequest(invoice.Id);

        _invoiceRepositoryMock.GetByIdAsync(invoice.Id).Returns(invoice);
        _clientRepositoryMock.GetByIdAsync(invoice.ClientId).Returns(client);
        _invoiceRepositoryMock.GetLastInvoiceNumberAsync().Returns("2026/001");
        _numberingServiceMock.GenerateNextInvoiceNumber("2026/001").Returns("2026/002");
        _invoiceRepositoryMock.UpdateAsync(Arg.Any<Invoice>())
            .Returns(callInfo => callInfo.Arg<Invoice>());

        // Act
        var response = await _sut.Execute(request);

        // Assert
        response.Invoice.Status.Should().Be(InvoiceStatus.Issued);
        response.InvoiceNumber.Should().Be("2026/002");
    }

    [Fact]
    public async Task Execute_ValidDraftInvoice_CalculatesTotalsBeforeIssuing()
    {
        // Arrange
        var invoice = CreateDraftInvoice();
        var client = CreateClient(invoice.ClientId);
        var request = CreateRequest(invoice.Id);

        _invoiceRepositoryMock.GetByIdAsync(invoice.Id).Returns(invoice);
        _clientRepositoryMock.GetByIdAsync(invoice.ClientId).Returns(client);
        _invoiceRepositoryMock.GetLastInvoiceNumberAsync().Returns((string?)null);
        _numberingServiceMock.GenerateNextInvoiceNumber(null).Returns("2026/001");
        _invoiceRepositoryMock.UpdateAsync(Arg.Any<Invoice>())
            .Returns(callInfo => callInfo.Arg<Invoice>());

        // Act
        await _sut.Execute(request);

        // Assert -- verify calculation was called
        _calculationServiceMock.Received(1).CalculateInvoiceTotals(
            Arg.Is<Invoice>(i => i.Id == invoice.Id));
    }

    [Fact]
    public async Task Execute_ValidDraftInvoice_PersistsUpdatedInvoice()
    {
        // Arrange
        var invoice = CreateDraftInvoice();
        var client = CreateClient(invoice.ClientId);
        var request = CreateRequest(invoice.Id);

        _invoiceRepositoryMock.GetByIdAsync(invoice.Id).Returns(invoice);
        _clientRepositoryMock.GetByIdAsync(invoice.ClientId).Returns(client);
        _invoiceRepositoryMock.GetLastInvoiceNumberAsync().Returns((string?)null);
        _numberingServiceMock.GenerateNextInvoiceNumber(null).Returns("2026/001");
        _invoiceRepositoryMock.UpdateAsync(Arg.Any<Invoice>())
            .Returns(callInfo => callInfo.Arg<Invoice>());

        // Act
        await _sut.Execute(request);

        // Assert -- verify persistence
        await _invoiceRepositoryMock.Received(1).UpdateAsync(
            Arg.Is<Invoice>(i =>
                i.Status == InvoiceStatus.Issued &&
                i.InvoiceNumber == "2026/001"));
    }

    [Fact]
    public async Task Execute_FirstInvoice_AssignsFirstNumber()
    {
        // Arrange -- no previous invoices
        var invoice = CreateDraftInvoice();
        var client = CreateClient(invoice.ClientId);
        var request = CreateRequest(invoice.Id);

        _invoiceRepositoryMock.GetByIdAsync(invoice.Id).Returns(invoice);
        _clientRepositoryMock.GetByIdAsync(invoice.ClientId).Returns(client);
        _invoiceRepositoryMock.GetLastInvoiceNumberAsync().Returns((string?)null);
        _numberingServiceMock.GenerateNextInvoiceNumber(null).Returns("2026/001");
        _invoiceRepositoryMock.UpdateAsync(Arg.Any<Invoice>())
            .Returns(callInfo => callInfo.Arg<Invoice>());

        // Act
        var response = await _sut.Execute(request);

        // Assert
        response.InvoiceNumber.Should().Be("2026/001");
    }

    #endregion

    // ── Phase 3: Execution Edge Cases ───────────────────────────────

    #region Execution Edge Cases

    [Fact]
    public async Task Execute_ValidInvoice_SetsClientOnInvoiceBeforeCalculation()
    {
        // Arrange
        var invoice = CreateDraftInvoice();
        var client = CreateClient(invoice.ClientId);
        var request = CreateRequest(invoice.Id);

        _invoiceRepositoryMock.GetByIdAsync(invoice.Id).Returns(invoice);
        _clientRepositoryMock.GetByIdAsync(invoice.ClientId).Returns(client);
        _invoiceRepositoryMock.GetLastInvoiceNumberAsync().Returns("2026/005");
        _numberingServiceMock.GenerateNextInvoiceNumber("2026/005").Returns("2026/006");
        _invoiceRepositoryMock.UpdateAsync(Arg.Any<Invoice>())
            .Returns(callInfo => callInfo.Arg<Invoice>());

        // Act
        await _sut.Execute(request);

        // Assert -- client must be set on invoice for correct calculation
        _calculationServiceMock.Received(1).CalculateInvoiceTotals(
            Arg.Is<Invoice>(i => i.Client == client));
    }

    #endregion

    // ── Interaction Verification ────────────────────────────────────

    #region Interaction Verification

    [Fact]
    public async Task Execute_InvoiceNotFound_DoesNotCallRepository()
    {
        // Arrange
        var request = CreateRequest(Guid.NewGuid());
        _invoiceRepositoryMock.GetByIdAsync(request.InvoiceId).Returns((Invoice?)null);

        // Act
        var act = () => _sut.Execute(request);
        await act.Should().ThrowAsync<NotFoundException>();

        // Assert -- should NOT reach persistence
        await _invoiceRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Invoice>());
        _calculationServiceMock.DidNotReceive().CalculateInvoiceTotals(Arg.Any<Invoice>());
    }

    [Fact]
    public async Task Execute_ValidationFails_DoesNotPersist()
    {
        // Arrange
        var invoice = CreateDraftInvoice();
        invoice.Status = InvoiceStatus.Paid; // Cannot transition to Issued
        var client = CreateClient(invoice.ClientId);
        var request = CreateRequest(invoice.Id);

        _invoiceRepositoryMock.GetByIdAsync(invoice.Id).Returns(invoice);
        _clientRepositoryMock.GetByIdAsync(invoice.ClientId).Returns(client);
        _invoiceRepositoryMock.GetLastInvoiceNumberAsync().Returns("2026/001");

        // Act
        var act = () => _sut.Execute(request);
        await act.Should().ThrowAsync<ForbiddenOperationException>();

        // Assert -- should NOT reach persistence
        await _invoiceRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Invoice>());
    }

    #endregion
}
```

## Test Structure Rules

### File Organization

```
tests/Fatturazione.Domain.Tests/
  UseCases/
    IssueInvoiceTests.cs
    CreateCreditNoteTests.cs
    TransitionInvoiceTests.cs
  Services/
    InvoiceCalculationServiceTests.cs   (existing)
    CreditNoteServiceTests.cs           (existing)
  Validators/
    InvoiceValidatorTests.cs            (existing)
```

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Test class | `{SubjectUnderTest}Tests` | `IssueInvoiceTests` |
| Test method | `{Method}_{Scenario}_{ExpectedResult}` | `Execute_InvoiceNotFound_ThrowsNotFoundException` |
| Mock fields | `_{name}Mock` or `_{name}ServiceMock` | `_invoiceRepositoryMock` |
| SUT field | `_sut` | `private readonly IssueInvoice _sut;` |
| Helper methods | Descriptive static methods | `CreateDraftInvoice()` |

### Region Structure

Organize tests into `#region` blocks matching the three phases:

```csharp
#region Helper Methods
// Shared factory methods for test data
#endregion

#region Load Failures
// Tests for Phase 1 failures (NotFoundException)
#endregion

#region Validation Failures
// Tests for Phase 2 failures (ForbiddenOperationException, InvalidInputException)
#endregion

#region Execution Success
// Tests for Phase 3 happy paths
#endregion

#region Execution Edge Cases
// Tests for Phase 3 boundary conditions
#endregion

#region Interaction Verification
// Tests that verify mock interactions (Received/DidNotReceive)
#endregion
```

### What to Test for Each Phase

#### Phase 1 (Load) Tests

- Entity not found -> `ThrowAsync<NotFoundException>`
- Each required entity gets its own not-found test
- Verify that downstream phases are NOT executed when load fails

```csharp
[Fact]
public async Task Execute_InvoiceNotFound_ThrowsNotFoundException()
{
    // Arrange
    _invoiceRepositoryMock.GetByIdAsync(Arg.Any<Guid>()).Returns((Invoice?)null);

    // Act
    var act = () => _sut.Execute(request);

    // Assert
    await act.Should().ThrowAsync<NotFoundException>();
    await _invoiceRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Invoice>());
}
```

#### Phase 2 (Validate) Tests

- Invalid state transition -> `ThrowAsync<ForbiddenOperationException>`
- Invalid input data -> `ThrowAsync<InvalidInputException>`
- Use `[Theory]` with `[InlineData]` for multiple invalid states
- Verify that persistence is NOT called when validation fails

```csharp
[Theory]
[InlineData(InvoiceStatus.Sent)]
[InlineData(InvoiceStatus.Paid)]
[InlineData(InvoiceStatus.Cancelled)]
public async Task Execute_InvalidStatus_ThrowsForbiddenOperationException(
    InvoiceStatus status)
{
    // Arrange
    invoice.Status = status;
    // ... setup mocks ...

    // Act
    var act = () => _sut.Execute(request);

    // Assert
    await act.Should().ThrowAsync<ForbiddenOperationException>();
    await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Invoice>());
}
```

#### Phase 3 (Execute) Tests

- Happy path returns correct response
- Calculation services are called with correct arguments
- Persistence receives the correctly mutated entity
- Side effects (if any) are triggered
- Edge cases: first invoice, null previous number, etc.

```csharp
[Fact]
public async Task Execute_ValidInvoice_PersistsWithCorrectStatus()
{
    // Arrange
    // ... setup all mocks for happy path ...

    // Act
    var response = await _sut.Execute(request);

    // Assert
    response.Invoice.Status.Should().Be(InvoiceStatus.Issued);
    await _repositoryMock.Received(1).UpdateAsync(
        Arg.Is<Invoice>(i => i.Status == InvoiceStatus.Issued));
}
```

## Mock Setup Patterns

### Repository Mocks

```csharp
// Entity found
_invoiceRepositoryMock.GetByIdAsync(invoiceId).Returns(invoice);

// Entity not found
_invoiceRepositoryMock.GetByIdAsync(Arg.Any<Guid>()).Returns((Invoice?)null);

// Persist and return
_invoiceRepositoryMock.UpdateAsync(Arg.Any<Invoice>())
    .Returns(callInfo => callInfo.Arg<Invoice>());

// Verify persistence
await _invoiceRepositoryMock.Received(1).UpdateAsync(
    Arg.Is<Invoice>(i => i.Status == InvoiceStatus.Issued));

// Verify NOT persisted
await _invoiceRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Invoice>());
```

### Service Mocks

```csharp
// Calculation service (void method -- just verify call)
_calculationServiceMock.Received(1).CalculateInvoiceTotals(
    Arg.Is<Invoice>(i => i.Client != null));

// Service with return value
_numberingServiceMock.GenerateNextInvoiceNumber("2026/001").Returns("2026/002");

// Service returns based on input
_ritenutaServiceMock.AppliesRitenuta(client).Returns(true);
_ritenutaServiceMock.CalculateRitenuta(1000m, client).Returns(200m);
```

### Logger Mock

```csharp
// Logger is injected but typically not asserted on (NSubstitute limitation with extension methods)
// Just ensure it is provided to avoid NullReferenceException
_loggerMock = Substitute.For<ILogger<IssueInvoice>>();
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run only use case tests
dotnet test --filter "FullyQualifiedName~UseCases"

# Run a specific test class
dotnet test --filter "FullyQualifiedName~IssueInvoiceTests"

# Run with verbose output
dotnet test --verbosity normal
```

## Checklist

- [ ] Test file created at `tests/Fatturazione.Domain.Tests/UseCases/{UseCaseName}Tests.cs`
- [ ] Test class matches naming convention: `{UseCaseName}Tests`
- [ ] Constructor creates all mocks and SUT
- [ ] Helper methods create valid test data
- [ ] Load failure tests: one per required entity
- [ ] Validation failure tests: one per business rule, including `[Theory]` for enum-based rules
- [ ] Execution success tests: happy path returns correct response
- [ ] Interaction verification: failed phases do not trigger downstream work
- [ ] All tests use Arrange-Act-Assert with comments
- [ ] All tests use `FluentAssertions` (`.Should()`)
- [ ] All async tests use `await act.Should().ThrowAsync<T>()` pattern
- [ ] Tests run green with `dotnet test`
