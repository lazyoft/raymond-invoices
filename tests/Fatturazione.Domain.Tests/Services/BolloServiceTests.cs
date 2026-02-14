using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Services;

/// <summary>
/// Tests for BolloService (Imposta di Bollo per DPR 642/72 Art. 13)
/// </summary>
public class BolloServiceTests
{
    private readonly BolloService _sut;

    public BolloServiceTests()
    {
        _sut = new BolloService();
    }

    [Fact]
    public void RequiresBollo_Forfettario_Over7747_ReturnsTrue()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = true,
            ImponibileTotal = 100.00m
        };

        // Act
        var result = _sut.RequiresBollo(invoice);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresBollo_Forfettario_Under7747_ReturnsFalse()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = true,
            ImponibileTotal = 50.00m
        };

        // Act
        var result = _sut.RequiresBollo(invoice);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RequiresBollo_Forfettario_Exactly7747_ReturnsFalse()
    {
        // Arrange - Per DPR 642/72 Art. 13: must exceed 77.47, not equal
        var invoice = new Invoice
        {
            IsRegimeForfettario = true,
            ImponibileTotal = 77.47m
        };

        // Act
        var result = _sut.RequiresBollo(invoice);

        // Assert
        result.Should().BeFalse("threshold must be exceeded, not equaled");
    }

    [Fact]
    public void RequiresBollo_NonForfettario_ReturnsFalse()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = false,
            ImponibileTotal = 1000.00m
        };

        // Act
        var result = _sut.RequiresBollo(invoice);

        // Assert
        result.Should().BeFalse("invoices with IVA do not require stamp duty");
    }

    [Fact]
    public void CalculateBollo_WhenRequired_Returns200()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = true,
            ImponibileTotal = 1000.00m
        };

        // Act
        var result = _sut.CalculateBollo(invoice);

        // Assert
        result.Should().Be(2.00m);
    }

    [Fact]
    public void CalculateBollo_WhenNotRequired_ReturnsZero()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = true,
            ImponibileTotal = 50.00m
        };

        // Act
        var result = _sut.CalculateBollo(invoice);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateBollo_NonForfettario_ReturnsZero()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = false,
            ImponibileTotal = 1000.00m
        };

        // Act
        var result = _sut.CalculateBollo(invoice);

        // Assert
        result.Should().Be(0m);
    }
}
