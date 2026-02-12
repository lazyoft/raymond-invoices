using Fatturazione.Domain.Validators;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Validators;

/// <summary>
/// Tests for PartitaIvaValidator
/// Algorithm conforms to official Italian Partita IVA checksum validation
/// </summary>
public class PartitaIvaValidatorTests
{
    [Fact]
    public void Validate_WithValidPartitaIva_ReturnsTrue()
    {
        // Arrange - valid Partita IVA with correct checksum
        var validPartitaIva = "12345678903";

        // Act
        var result = PartitaIvaValidator.Validate(validPartitaIva);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyOrWhitespace_ReturnsFalse(string? partitaIva)
    {
        // Act
        var result = PartitaIvaValidator.Validate(partitaIva);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("123456789")] // too short
    [InlineData("1234567890123")] // too long
    [InlineData("123456")]
    public void Validate_WithIncorrectLength_ReturnsFalse(string partitaIva)
    {
        // Act
        var result = PartitaIvaValidator.Validate(partitaIva);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("1234567890A")] // contains letter
    [InlineData("ABCDEFGHIJK")] // all letters
    [InlineData("1234567-890")] // only this one has special chars that won't be removed
    public void Validate_WithNonNumericCharacters_ReturnsFalse(string partitaIva)
    {
        // Act
        var result = PartitaIvaValidator.Validate(partitaIva);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithIncorrectChecksum_ReturnsFalse()
    {
        // Arrange - valid format but wrong checksum (last digit should be 3, not 1)
        var invalidPartitaIva = "12345678901";

        // Act
        var result = PartitaIvaValidator.Validate(invalidPartitaIva);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("123 4567 8903")]
    [InlineData("123-4567-8903")]
    [InlineData("  12345678903  ")]
    public void Validate_WithSpacesOrDashes_CleansAndValidatesCorrectly(string partitaIva)
    {
        // Act
        var result = PartitaIvaValidator.Validate(partitaIva);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "Partita IVA è obbligatoria")]
    [InlineData("", "Partita IVA è obbligatoria")]
    [InlineData("   ", "Partita IVA è obbligatoria")]
    [InlineData("123456789", "Partita IVA deve essere di 11 cifre")]
    [InlineData("1234567890123", "Partita IVA deve essere di 11 cifre")]
    [InlineData("1234567890A", "Partita IVA deve contenere solo numeri")]
    [InlineData("ABCDEFGHIJK", "Partita IVA deve contenere solo numeri")]
    [InlineData("12345678901", "Partita IVA non valida (checksum errato)")]
    public void GetValidationError_ReturnsCorrectErrorMessage(string? partitaIva, string expectedError)
    {
        // Act
        var result = PartitaIvaValidator.GetValidationError(partitaIva);

        // Assert
        result.Should().Be(expectedError);
    }

    [Fact]
    public void GetValidationError_WithValidPartitaIva_ReturnsEmptyString()
    {
        // Arrange
        var validPartitaIva = "12345678903";

        // Act
        var result = PartitaIvaValidator.GetValidationError(validPartitaIva);

        // Assert
        result.Should().BeEmpty();
    }
}
