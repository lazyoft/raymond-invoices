using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using FluentAssertions;
using NSubstitute;

namespace Fatturazione.Domain.Tests.Services;

/// <summary>
/// Tests for InvoiceCalculationService
/// </summary>
public class InvoiceCalculationServiceTests
{
    private readonly IRitenutaService _ritenutaServiceMock;
    private readonly InvoiceCalculationService _sut;

    public InvoiceCalculationServiceTests()
    {
        _ritenutaServiceMock = Substitute.For<IRitenutaService>();
        _sut = new InvoiceCalculationService(_ritenutaServiceMock);
    }

    #region CalculateItemTotals Tests

    [Fact]
    public void CalculateItemTotals_SimpleItem_CalculatesCorrectly()
    {
        // Arrange
        var item = new InvoiceItem
        {
            Quantity = 2,
            UnitPrice = 100,
            IvaRate = IvaRate.Standard // 22%
        };

        // Act
        _sut.CalculateItemTotals(item);

        // Assert
        item.Imponibile.Should().Be(200m);
        item.IvaAmount.Should().Be(44m);
        item.Total.Should().Be(244m);
    }

    [Fact]
    public void CalculateItemTotals_WithReducedIva_CalculatesCorrectly()
    {
        // Arrange
        var item = new InvoiceItem
        {
            Quantity = 1,
            UnitPrice = 500,
            IvaRate = IvaRate.Reduced // 10%
        };

        // Act
        _sut.CalculateItemTotals(item);

        // Assert
        item.Imponibile.Should().Be(500m);
        item.IvaAmount.Should().Be(50m);
        item.Total.Should().Be(550m);
    }

    [Fact]
    public void CalculateItemTotals_WithSuperReducedIva_CalculatesCorrectly()
    {
        // Arrange
        var item = new InvoiceItem
        {
            Quantity = 3,
            UnitPrice = 100,
            IvaRate = IvaRate.SuperReduced // 4%
        };

        // Act
        _sut.CalculateItemTotals(item);

        // Assert
        item.Imponibile.Should().Be(300m);
        item.IvaAmount.Should().Be(12m);
        item.Total.Should().Be(312m);
    }

    [Fact]
    public void CalculateItemTotals_WithZeroIva_CalculatesCorrectly()
    {
        // Arrange
        var item = new InvoiceItem
        {
            Quantity = 2,
            UnitPrice = 150,
            IvaRate = IvaRate.Zero // 0%
        };

        // Act
        _sut.CalculateItemTotals(item);

        // Assert
        item.Imponibile.Should().Be(300m);
        item.IvaAmount.Should().Be(0m);
        item.Total.Should().Be(300m);
    }

    [Fact]
    public void CalculateItemTotals_WithDiscountPercentage_CalculatesCorrectly()
    {
        // Arrange
        var item = new InvoiceItem
        {
            Quantity = 10,
            UnitPrice = 100,
            DiscountPercentage = 10,
            IvaRate = IvaRate.Standard
        };

        // Act
        _sut.CalculateItemTotals(item);

        // Assert
        item.Imponibile.Should().Be(900m); // 1000 - 10%
        item.IvaAmount.Should().Be(198m); // 900 * 22%
        item.Total.Should().Be(1098m);
    }

    [Fact]
    public void CalculateItemTotals_WithDiscountAmount_CalculatesCorrectly()
    {
        // Arrange
        var item = new InvoiceItem
        {
            Quantity = 10,
            UnitPrice = 100,
            DiscountPercentage = 0,
            DiscountAmount = 50,
            IvaRate = IvaRate.Standard
        };

        // Act
        _sut.CalculateItemTotals(item);

        // Assert
        item.Imponibile.Should().Be(950m); // 1000 - 50
        item.IvaAmount.Should().Be(209m); // 950 * 22%
        item.Total.Should().Be(1159m);
    }

    #endregion

    #region CalculateInvoiceTotals Tests

    [Fact]
    public void CalculateInvoiceTotals_WithNoItems_SetsAllTotalsToZero()
    {
        // Arrange
        var invoice = new Invoice
        {
            Items = new List<InvoiceItem>()
        };

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.ImponibileTotal.Should().Be(0);
        invoice.IvaTotal.Should().Be(0);
        invoice.SubTotal.Should().Be(0);
        invoice.RitenutaAmount.Should().Be(0);
        invoice.TotalDue.Should().Be(0);
    }

    [Fact]
    public void CalculateInvoiceTotals_WithNullItems_SetsAllTotalsToZero()
    {
        // Arrange
        var invoice = new Invoice
        {
            Items = null!
        };

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.ImponibileTotal.Should().Be(0);
        invoice.IvaTotal.Should().Be(0);
        invoice.SubTotal.Should().Be(0);
        invoice.RitenutaAmount.Should().Be(0);
        invoice.TotalDue.Should().Be(0);
    }

    [Fact]
    public void CalculateInvoiceTotals_WithSingleItem_CalculatesCorrectly()
    {
        // Arrange
        var invoice = new Invoice
        {
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Quantity = 1,
                    UnitPrice = 1000,
                    IvaRate = IvaRate.Standard
                }
            }
        };

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.ImponibileTotal.Should().Be(1000m);
        invoice.IvaTotal.Should().Be(220m);
        invoice.SubTotal.Should().Be(1220m);
        invoice.TotalDue.Should().Be(1220m); // No ritenuta without client
    }

    [Fact]
    public void CalculateInvoiceTotals_WithMultipleItemsDifferentRates_AggregatesCorrectly()
    {
        // Arrange
        var invoice = new Invoice
        {
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard },
                new InvoiceItem { Quantity = 2, UnitPrice = 500, IvaRate = IvaRate.Reduced },
                new InvoiceItem { Quantity = 3, UnitPrice = 100, IvaRate = IvaRate.SuperReduced }
            }
        };

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        // Imponibile: 1000 + 1000 + 300 = 2300
        invoice.ImponibileTotal.Should().Be(2300m);
        // IVA: 220 (22%) + 100 (10%) + 12 (4%) = 332
        invoice.IvaTotal.Should().Be(332m);
        // SubTotal: 2300 + 332 = 2632
        invoice.SubTotal.Should().Be(2632m);
        invoice.TotalDue.Should().Be(2632m); // No ritenuta without client
    }

    [Fact]
    public void CalculateInvoiceTotals_WithClientNotSubjectToRitenuta_NoRitenutaApplied()
    {
        // Arrange
        var client = new Client
        {
            SubjectToRitenuta = false,
            RitenutaPercentage = 20m
        };

        var invoice = new Invoice
        {
            Client = client,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        _ritenutaServiceMock.AppliesRitenuta(client).Returns(false);

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.RitenutaAmount.Should().Be(0);
        invoice.TotalDue.Should().Be(1220m); // SubTotal without ritenuta
    }

    [Fact]
    public void CalculateInvoiceTotals_WithNullClient_NoRitenutaApplied()
    {
        // Arrange
        var invoice = new Invoice
        {
            Client = null,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.RitenutaAmount.Should().Be(0);
        invoice.TotalDue.Should().Be(1220m);
    }

    [Fact]
    [Trait("Category", "KnownBug")]
    public void CalculateInvoiceTotals_WithClientSubjectToRitenuta_CalculatesOnSubTotal()
    {
        // BUG: la ritenuta dovrebbe essere calcolata su ImponibileTotal (1000), 
        // non su SubTotal (1220). Valore corretto: RitenutaAmount = 200, TotalDue = 1020

        // Arrange
        var client = new Client
        {
            SubjectToRitenuta = true,
            RitenutaPercentage = 20m
        };

        var invoice = new Invoice
        {
            Client = client,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        _ritenutaServiceMock.AppliesRitenuta(client).Returns(true);
        _ritenutaServiceMock.CalculateRitenuta(Arg.Any<decimal>(), 20m).Returns(244m);

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert - documents current (buggy) behavior
        invoice.ImponibileTotal.Should().Be(1000m);
        invoice.IvaTotal.Should().Be(220m);
        invoice.SubTotal.Should().Be(1220m);
        invoice.RitenutaAmount.Should().Be(244m); // Bug: should be 200
        invoice.TotalDue.Should().Be(976m); // Bug: should be 1020
    }

    #endregion

    #region CalculateRitenutaAmount Tests

    [Fact]
    [Trait("Category", "KnownBug")]
    public void CalculateRitenutaAmount_PassesSubTotalToService_NotImponibile()
    {
        // BUG: dovrebbe passare ImponibileTotal, non SubTotal

        // Arrange
        var client = new Client
        {
            SubjectToRitenuta = true,
            RitenutaPercentage = 20m
        };

        var invoice = new Invoice
        {
            Client = client,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        _ritenutaServiceMock.AppliesRitenuta(client).Returns(true);

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert - verify that SubTotal (1220) is passed, not ImponibileTotal (1000)
        _ritenutaServiceMock.Received(1).CalculateRitenuta(1220m, 20m);
    }

    #endregion

    #region CalculateIvaByRate Tests

    [Fact]
    public void CalculateIvaByRate_WithDifferentRates_ReturnsCorrectBreakdown()
    {
        // Arrange
        var invoice = new Invoice
        {
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard },
                new InvoiceItem { Quantity = 2, UnitPrice = 500, IvaRate = IvaRate.Reduced },
                new InvoiceItem { Quantity = 3, UnitPrice = 100, IvaRate = IvaRate.SuperReduced }
            }
        };

        // Calculate item totals first
        foreach (var item in invoice.Items)
        {
            _sut.CalculateItemTotals(item);
        }

        // Act
        var result = _sut.CalculateIvaByRate(invoice);

        // Assert
        result.Should().HaveCount(3);
        result[IvaRate.Standard].Should().Be(220m);
        result[IvaRate.Reduced].Should().Be(100m);
        result[IvaRate.SuperReduced].Should().Be(12m);
    }

    [Fact]
    public void CalculateIvaByRate_WithSameRate_SumsIvaAmounts()
    {
        // Arrange
        var invoice = new Invoice
        {
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard },
                new InvoiceItem { Quantity = 2, UnitPrice = 500, IvaRate = IvaRate.Standard }
            }
        };

        // Calculate item totals first
        foreach (var item in invoice.Items)
        {
            _sut.CalculateItemTotals(item);
        }

        // Act
        var result = _sut.CalculateIvaByRate(invoice);

        // Assert
        result.Should().HaveCount(1);
        result[IvaRate.Standard].Should().Be(440m); // 220 + 220
    }

    #endregion
}
