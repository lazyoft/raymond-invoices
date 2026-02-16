using Fatturazione.Domain.Services;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Services;

/// <summary>
/// Tests for DocumentDiscountService per Specifiche FatturaPA, blocco 2.1.1.8
/// Sconti/maggiorazioni a livello documento
/// </summary>
public class DocumentDiscountServiceTests
{
    private readonly DocumentDiscountService _sut;

    public DocumentDiscountServiceTests()
    {
        _sut = new DocumentDiscountService();
    }

    #region Percentage discount only

    [Fact]
    public void ApplyDocumentDiscount_10Percent_Of1000_Returns900()
    {
        // Arrange: 10% discount on 1000 EUR
        // Expected: 1000 * (1 - 0.10) = 900
        var result = _sut.ApplyDocumentDiscount(1000m, 10m, 0m);

        // Assert
        result.Should().Be(900.00m);
    }

    [Fact]
    public void ApplyDocumentDiscount_0Percent_Returns_OriginalAmount()
    {
        // Arrange: 0% discount (no discount)
        var result = _sut.ApplyDocumentDiscount(1000m, 0m, 0m);

        // Assert
        result.Should().Be(1000.00m);
    }

    [Fact]
    public void ApplyDocumentDiscount_100Percent_ReturnsZero()
    {
        // Arrange: 100% discount
        var result = _sut.ApplyDocumentDiscount(1000m, 100m, 0m);

        // Assert
        result.Should().Be(0.00m);
    }

    [Fact]
    public void ApplyDocumentDiscount_PercentageOnly_RoundsTo2Decimals()
    {
        // Arrange: 33.33% of 100 = 66.67
        // 100 * (1 - 0.3333) = 100 * 0.6667 = 66.67
        var result = _sut.ApplyDocumentDiscount(100m, 33.33m, 0m);

        // Assert
        result.Should().Be(66.67m);
    }

    #endregion

    #region Fixed discount only

    [Fact]
    public void ApplyDocumentDiscount_50Fixed_Of1000_Returns950()
    {
        // Arrange: 50 EUR flat discount on 1000 EUR
        var result = _sut.ApplyDocumentDiscount(1000m, 0m, 50m);

        // Assert
        result.Should().Be(950.00m);
    }

    [Fact]
    public void ApplyDocumentDiscount_FixedZero_ReturnsOriginalAmount()
    {
        // Arrange: 0 EUR flat discount
        var result = _sut.ApplyDocumentDiscount(500m, 0m, 0m);

        // Assert
        result.Should().Be(500.00m);
    }

    [Fact]
    public void ApplyDocumentDiscount_FixedEqualToTotal_ReturnsZero()
    {
        // Arrange: discount exactly equal to imponibile
        var result = _sut.ApplyDocumentDiscount(500m, 0m, 500m);

        // Assert
        result.Should().Be(0.00m);
    }

    [Fact]
    public void ApplyDocumentDiscount_FixedExceedsTotal_ReturnsZero()
    {
        // Arrange: fixed discount larger than imponibile
        var result = _sut.ApplyDocumentDiscount(100m, 0m, 200m);

        // Assert
        result.Should().Be(0.00m, "result must not go below zero");
    }

    #endregion

    #region Combined discounts (percentage + fixed)

    [Fact]
    public void ApplyDocumentDiscount_10Percent_Plus50Fixed_Of1000_Returns850()
    {
        // Arrange: 10% + 50 EUR on 1000 EUR
        // Step 1: 1000 * (1 - 0.10) = 900
        // Step 2: 900 - 50 = 850
        var result = _sut.ApplyDocumentDiscount(1000m, 10m, 50m);

        // Assert
        result.Should().Be(850.00m);
    }

    [Fact]
    public void ApplyDocumentDiscount_Combined_OrderMatters_PercentageFirst()
    {
        // Arrange: 50% + 600 EUR on 1000 EUR
        // If percentage first: 1000 * 0.50 = 500, then 500 - 600 = -100 → 0
        // If fixed first: 1000 - 600 = 400, then 400 * 0.50 = 200 (different!)
        // We expect percentage first, so result should be 0
        var result = _sut.ApplyDocumentDiscount(1000m, 50m, 600m);

        // Assert
        result.Should().Be(0.00m, "percentage is applied first, then fixed amount");
    }

    [Fact]
    public void ApplyDocumentDiscount_Combined_NegativeResult_ReturnsZero()
    {
        // Arrange: 90% discount + 200 EUR on 1000 EUR
        // Step 1: 1000 * 0.10 = 100
        // Step 2: 100 - 200 = -100 → 0
        var result = _sut.ApplyDocumentDiscount(1000m, 90m, 200m);

        // Assert
        result.Should().Be(0.00m, "result must not go below zero");
    }

    [Fact]
    public void ApplyDocumentDiscount_Combined_RoundsTo2Decimals()
    {
        // Arrange: 15% + 10.50 EUR on 333.33 EUR
        // Step 1: 333.33 * (1 - 0.15) = 333.33 * 0.85 = 283.3305
        // Step 2: 283.3305 - 10.50 = 272.8305 → 272.83
        var result = _sut.ApplyDocumentDiscount(333.33m, 15m, 10.50m);

        // Assert
        result.Should().Be(272.83m);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void ApplyDocumentDiscount_BothZero_ReturnsOriginalAmount()
    {
        // Arrange: no discount at all
        var result = _sut.ApplyDocumentDiscount(1234.56m, 0m, 0m);

        // Assert
        result.Should().Be(1234.56m);
    }

    [Fact]
    public void ApplyDocumentDiscount_ZeroImponibile_ReturnsZero()
    {
        // Arrange: imponibile is zero
        var result = _sut.ApplyDocumentDiscount(0m, 10m, 50m);

        // Assert
        result.Should().Be(0.00m);
    }

    [Fact]
    public void ApplyDocumentDiscount_SmallAmount_PrecisionMaintained()
    {
        // Arrange: 1% of 0.50 EUR = 0.005 → rounds to 0.50 (0.50 * 0.99 = 0.495 → 0.50)
        var result = _sut.ApplyDocumentDiscount(0.50m, 1m, 0m);

        // Assert
        result.Should().Be(0.50m);
    }

    #endregion
}
