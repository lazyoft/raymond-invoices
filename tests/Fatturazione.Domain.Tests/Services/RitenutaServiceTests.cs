using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Services;

/// <summary>
/// Tests for RitenutaService
/// </summary>
public class RitenutaServiceTests
{
    private readonly RitenutaService _sut;

    public RitenutaServiceTests()
    {
        _sut = new RitenutaService();
    }

    [Fact]
    public void AppliesRitenuta_WhenClientSubjectToRitenutaIsTrue_ReturnsTrue()
    {
        // Arrange
        var client = new Client
        {
            SubjectToRitenuta = true
        };

        // Act
        var result = _sut.AppliesRitenuta(client);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AppliesRitenuta_WhenClientSubjectToRitenutaIsFalse_ReturnsFalse()
    {
        // Arrange
        var client = new Client
        {
            SubjectToRitenuta = false
        };

        // Act
        var result = _sut.AppliesRitenuta(client);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CalculateRitenuta_WithImponibile1000AndPercentage20_Returns200()
    {
        // Arrange
        decimal imponibile = 1000m;
        decimal percentage = 20m;

        // Act
        var result = _sut.CalculateRitenuta(imponibile, percentage);

        // Assert
        result.Should().Be(200m);
    }

    [Fact]
    public void CalculateRitenuta_WithImponibileZero_ReturnsZero()
    {
        // Arrange
        decimal imponibile = 0m;
        decimal percentage = 20m;

        // Act
        var result = _sut.CalculateRitenuta(imponibile, percentage);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateRitenuta_RoundsToTwoDecimals()
    {
        // Arrange
        decimal imponibile = 333.33m;
        decimal percentage = 20m;

        // Act
        var result = _sut.CalculateRitenuta(imponibile, percentage);

        // Assert
        result.Should().Be(66.67m);
    }

    [Theory]
    [InlineData(ClientType.Professional, 20.0)]
    [InlineData(ClientType.Company, 0.0)]
    [InlineData(ClientType.PublicAdministration, 0.0)]
    public void GetStandardRate_ReturnsCorrectRateForClientType(ClientType clientType, decimal expectedRate)
    {
        // Act
        var result = _sut.GetStandardRate(clientType);

        // Assert
        result.Should().Be(expectedRate);
    }
}
