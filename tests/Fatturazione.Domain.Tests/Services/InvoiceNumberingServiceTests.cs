using Fatturazione.Domain.Services;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Services;

/// <summary>
/// Tests for InvoiceNumberingService
/// </summary>
public class InvoiceNumberingServiceTests
{
    private readonly InvoiceNumberingService _sut;

    public InvoiceNumberingServiceTests()
    {
        _sut = new InvoiceNumberingService();
    }

    [Fact]
    public void GenerateNextInvoiceNumber_WithNull_ReturnsFirstInvoiceOfCurrentYear()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;

        // Act
        var result = _sut.GenerateNextInvoiceNumber(null);

        // Assert
        result.Should().Be($"{currentYear}/001");
    }

    [Fact]
    public void GenerateNextInvoiceNumber_WithEmptyString_ReturnsFirstInvoiceOfCurrentYear()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;

        // Act
        var result = _sut.GenerateNextInvoiceNumber("");

        // Assert
        result.Should().Be($"{currentYear}/001");
    }

    [Fact]
    public void GenerateNextInvoiceNumber_WithValidNumber_IncrementsSequence()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var lastNumber = "2026/001";

        // Act
        var result = _sut.GenerateNextInvoiceNumber(lastNumber);

        // Assert
        result.Should().Be($"{currentYear}/002");
    }

    [Fact]
    public void GenerateNextInvoiceNumber_WithPreviousYear_ResetsSequenceToOne()
    {
        // Per Art. 21 DPR 633/72, la numerazione riparte da 001 al cambio anno
        // Arrange
        var currentYear = DateTime.Now.Year;
        var lastNumber = "2025/005";

        // Act
        var result = _sut.GenerateNextInvoiceNumber(lastNumber);

        // Assert
        result.Should().Be($"{currentYear}/001");
    }

    [Fact]
    public void GenerateNextInvoiceNumber_WithSameYear_IncrementsNormally()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var lastNumber = $"{currentYear}/042";

        // Act
        var result = _sut.GenerateNextInvoiceNumber(lastNumber);

        // Assert
        result.Should().Be($"{currentYear}/043");
    }

    [Fact]
    public void GenerateNextInvoiceNumber_WithPreviousYear_HighSequence_ResetsToOne()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var lastNumber = "2024/999";

        // Act
        var result = _sut.GenerateNextInvoiceNumber(lastNumber);

        // Assert
        result.Should().Be($"{currentYear}/001");
    }

    [Fact]
    public void GenerateNextInvoiceNumber_WithMultipleYearsBack_ResetsToOne()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var lastNumber = "2020/050";

        // Act
        var result = _sut.GenerateNextInvoiceNumber(lastNumber);

        // Assert
        result.Should().Be($"{currentYear}/001");
    }

    [Fact]
    public void GenerateNextInvoiceNumber_WithInvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var invalidNumber = "invalid";

        // Act
        var act = () => _sut.GenerateNextInvoiceNumber(invalidNumber);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Invalid invoice number format: {invalidNumber}");
    }

    [Fact]
    public void ValidateInvoiceNumberFormat_WithValidFormat_ReturnsTrue()
    {
        // Arrange
        var validNumber = "2026/001";

        // Act
        var result = _sut.ValidateInvoiceNumberFormat(validNumber);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateInvoiceNumberFormat_WithInvalidSequenceDigits_ReturnsFalse()
    {
        // Arrange - only 1 digit instead of 3
        var invalidNumber = "2026/1";

        // Act
        var result = _sut.ValidateInvoiceNumberFormat(invalidNumber);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateInvoiceNumberFormat_WithNull_ReturnsFalse()
    {
        // Act
        var result = _sut.ValidateInvoiceNumberFormat(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateInvoiceNumberFormat_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = _sut.ValidateInvoiceNumberFormat("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetYearFromInvoiceNumber_WithValidNumber_ReturnsYear()
    {
        // Arrange
        var invoiceNumber = "2026/001";

        // Act
        var result = _sut.GetYearFromInvoiceNumber(invoiceNumber);

        // Assert
        result.Should().Be(2026);
    }

    [Fact]
    public void GetSequenceFromInvoiceNumber_WithValidNumber_ReturnsSequence()
    {
        // Arrange
        var invoiceNumber = "2026/005";

        // Act
        var result = _sut.GetSequenceFromInvoiceNumber(invoiceNumber);

        // Assert
        result.Should().Be(5);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("2026-001")]
    [InlineData("2026/1")]
    [InlineData("")]
    public void GetYearFromInvoiceNumber_WithInvalidFormat_ThrowsArgumentException(string invalidNumber)
    {
        // Act
        var act = () => _sut.GetYearFromInvoiceNumber(invalidNumber);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Invalid invoice number format: {invalidNumber}");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("2026-001")]
    [InlineData("2026/1")]
    [InlineData("")]
    public void GetSequenceFromInvoiceNumber_WithInvalidFormat_ThrowsArgumentException(string invalidNumber)
    {
        // Act
        var act = () => _sut.GetSequenceFromInvoiceNumber(invalidNumber);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Invalid invoice number format: {invalidNumber}");
    }
}
