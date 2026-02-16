using Fatturazione.Domain.Models;
using Fatturazione.Domain.Validators;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Validators;

/// <summary>
/// Tests for InvoiceValidator
/// </summary>
public class InvoiceValidatorTests
{
    #region Validate Tests

    [Fact]
    public void Validate_WithValidInvoice_ReturnsTrue()
    {
        // Arrange
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "Service", Quantity = 1, UnitPrice = 100 }
            }
        };

        // Act
        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyClientId_ReturnsError()
    {
        // Arrange
        var invoice = new Invoice
        {
            ClientId = Guid.Empty,
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30)
        };

        // Act
        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("ClientId è obbligatorio");
    }

    [Fact]
    public void Validate_WithDefaultInvoiceDate_ReturnsError()
    {
        // Arrange
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = default,
            DueDate = DateTime.Now.AddDays(30)
        };

        // Act
        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("InvoiceDate è obbligatoria");
    }

    [Fact]
    public void Validate_WithDefaultDueDate_ReturnsError()
    {
        // Arrange
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Now,
            DueDate = default
        };

        // Act
        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("DueDate è obbligatoria");
    }

    [Fact]
    public void Validate_WithDueDateBeforeInvoiceDate_ReturnsError()
    {
        // Arrange
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(-1)
        };

        // Act
        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("DueDate deve essere dopo InvoiceDate");
    }

    [Fact]
    public void Validate_WithItemMissingDescription_ReturnsError()
    {
        // Arrange
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "" }
            }
        };

        // Act
        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Item 1: Description è obbligatoria");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var invoice = new Invoice
        {
            ClientId = Guid.Empty,
            InvoiceDate = default,
            DueDate = default,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "" }
            }
        };

        // Act
        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().HaveCount(5);
        errors.Should().Contain("ClientId è obbligatorio");
        errors.Should().Contain("InvoiceDate è obbligatoria");
        errors.Should().Contain("DueDate è obbligatoria");
        errors.Should().Contain("Item 1: Description è obbligatoria");
        errors.Should().Contain("Item 1: Quantity deve essere maggiore di 0");
    }

    [Fact]
    public void Validate_WithNoItems_ReturnsError()
    {
        // Art. 21 DPR 633/72 - fattura deve avere almeno un item
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = new List<InvoiceItem>()
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain("La fattura deve contenere almeno un item");
    }

    [Fact]
    public void Validate_WithNullItems_ReturnsError()
    {
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = null!
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain("La fattura deve contenere almeno un item");
    }

    [Fact]
    public void Validate_WithZeroQuantity_ReturnsError()
    {
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "Service", Quantity = 0, UnitPrice = 100 }
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain("Item 1: Quantity deve essere maggiore di 0");
    }

    [Fact]
    public void Validate_WithNegativeQuantity_ReturnsError()
    {
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "Service", Quantity = -1, UnitPrice = 100 }
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain("Item 1: Quantity deve essere maggiore di 0");
    }

    [Fact]
    public void Validate_WithNegativeUnitPrice_ReturnsError()
    {
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "Service", Quantity = 1, UnitPrice = -50 }
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain("Item 1: UnitPrice non può essere negativo");
    }

    [Fact]
    public void Validate_WithZeroUnitPrice_IsValid()
    {
        // Zero unit price is allowed (e.g., promotional items)
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "Free Sample", Quantity = 1, UnitPrice = 0 }
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
    }

    #endregion

    #region CanTransitionTo Tests - Valid Transitions

    [Theory]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Issued)]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Cancelled)]
    public void CanTransitionTo_FromDraft_AllowsIssuedAndCancelled(InvoiceStatus from, InvoiceStatus to)
    {
        // Act
        var result = InvoiceValidator.CanTransitionTo(from, to);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(InvoiceStatus.Issued, InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Issued, InvoiceStatus.Cancelled)]
    public void CanTransitionTo_FromIssued_AllowsSentAndCancelled(InvoiceStatus from, InvoiceStatus to)
    {
        // Act
        var result = InvoiceValidator.CanTransitionTo(from, to);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Overdue)]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Cancelled)]
    public void CanTransitionTo_FromSent_AllowsPaidOverdueAndCancelled(InvoiceStatus from, InvoiceStatus to)
    {
        // Act
        var result = InvoiceValidator.CanTransitionTo(from, to);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(InvoiceStatus.Overdue, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Overdue, InvoiceStatus.Cancelled)]
    public void CanTransitionTo_FromOverdue_AllowsPaidAndCancelled(InvoiceStatus from, InvoiceStatus to)
    {
        // Act
        var result = InvoiceValidator.CanTransitionTo(from, to);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region CanTransitionTo Tests - Invalid Transitions

    [Theory]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Overdue)]
    public void CanTransitionTo_FromDraft_RejectsInvalidTransitions(InvoiceStatus from, InvoiceStatus to)
    {
        // Act
        var result = InvoiceValidator.CanTransitionTo(from, to);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(InvoiceStatus.Issued, InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Issued, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Issued, InvoiceStatus.Overdue)]
    public void CanTransitionTo_FromIssued_RejectsInvalidTransitions(InvoiceStatus from, InvoiceStatus to)
    {
        // Act
        var result = InvoiceValidator.CanTransitionTo(from, to);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Issued)]
    public void CanTransitionTo_FromSent_RejectsInvalidTransitions(InvoiceStatus from, InvoiceStatus to)
    {
        // Act
        var result = InvoiceValidator.CanTransitionTo(from, to);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Issued)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Overdue)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Cancelled)]
    public void CanTransitionTo_FromPaid_RejectsAllTransitions(InvoiceStatus from, InvoiceStatus to)
    {
        // Paid is a terminal state
        // Act
        var result = InvoiceValidator.CanTransitionTo(from, to);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(InvoiceStatus.Cancelled, InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Cancelled, InvoiceStatus.Issued)]
    [InlineData(InvoiceStatus.Cancelled, InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Cancelled, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled, InvoiceStatus.Overdue)]
    public void CanTransitionTo_FromCancelled_RejectsAllTransitions(InvoiceStatus from, InvoiceStatus to)
    {
        // Cancelled is a terminal state
        // Act
        var result = InvoiceValidator.CanTransitionTo(from, to);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
