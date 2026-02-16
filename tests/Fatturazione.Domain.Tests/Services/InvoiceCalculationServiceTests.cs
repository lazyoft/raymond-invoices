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
    private readonly IBolloService _bolloServiceMock;
    private readonly InvoiceCalculationService _sut;

    public InvoiceCalculationServiceTests()
    {
        _ritenutaServiceMock = Substitute.For<IRitenutaService>();
        _bolloServiceMock = Substitute.For<IBolloService>();
        _sut = new InvoiceCalculationService(_ritenutaServiceMock, _bolloServiceMock);
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
    public void CalculateItemTotals_WithIntermediateIva_CalculatesCorrectly()
    {
        // Arrange - Prestazione socio-sanitaria cooperativa sociale
        var item = new InvoiceItem
        {
            Quantity = 10,
            UnitPrice = 200,
            IvaRate = IvaRate.Intermediate // 5%
        };

        // Act
        _sut.CalculateItemTotals(item);

        // Assert
        item.Imponibile.Should().Be(2000m);
        item.IvaAmount.Should().Be(100m); // 2000 * 5% = 100
        item.Total.Should().Be(2100m);
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
    public void CalculateInvoiceTotals_WithIntermediateIva_CalculatesCorrectly()
    {
        // Arrange
        var invoice = new Invoice
        {
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Intermediate }
            }
        };

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.ImponibileTotal.Should().Be(1000m);
        invoice.IvaTotal.Should().Be(50m); // 1000 * 5% = 50
        invoice.SubTotal.Should().Be(1050m);
        invoice.TotalDue.Should().Be(1050m); // No ritenuta without client
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
    public void CalculateInvoiceTotals_WithClientSubjectToRitenuta_CalculatesOnImponibile()
    {
        // Art. 25 DPR 600/73: la ritenuta si calcola sull'imponibile (netto IVA)

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
        _ritenutaServiceMock.CalculateRitenuta(1000m, 20m).Returns(200m);

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.ImponibileTotal.Should().Be(1000m);
        invoice.IvaTotal.Should().Be(220m);
        invoice.SubTotal.Should().Be(1220m);
        invoice.RitenutaAmount.Should().Be(200m);
        invoice.TotalDue.Should().Be(1020m);
    }

    #endregion

    #region CalculateRitenutaAmount Tests

    [Fact]
    public void CalculateRitenutaAmount_PassesImponibileTotalToService()
    {
        // Art. 25 DPR 600/73: verifica che venga passato ImponibileTotal al servizio ritenuta

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

        // Assert - verify that ImponibileTotal (1000) is passed, not SubTotal (1220)
        _ritenutaServiceMock.Received(1).CalculateRitenuta(1000m, 20m);
    }

    [Fact]
    public void CalculateInvoiceTotals_ProfessionalClient_RitenutaEndToEnd()
    {
        // End-to-end: Imponibile 5000, IVA 22% = 1100, SubTotal = 6100,
        // Ritenuta 20% of 5000 = 1000, TotalDue = 6100 - 1000 = 5100

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
                new InvoiceItem { Quantity = 5, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        _ritenutaServiceMock.AppliesRitenuta(client).Returns(true);
        _ritenutaServiceMock.CalculateRitenuta(5000m, 20m).Returns(1000m);

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.ImponibileTotal.Should().Be(5000m);
        invoice.IvaTotal.Should().Be(1100m);
        invoice.SubTotal.Should().Be(6100m);
        invoice.RitenutaAmount.Should().Be(1000m);
        invoice.TotalDue.Should().Be(5100m);
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

    [Fact]
    public void CalculateIvaByRate_WithAllFiveRates_ReturnsCorrectBreakdown()
    {
        // Arrange
        var invoice = new Invoice
        {
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard },
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Reduced },
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Intermediate },
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.SuperReduced },
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Zero }
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
        result.Should().HaveCount(5);
        result[IvaRate.Standard].Should().Be(220m);      // 1000 * 22%
        result[IvaRate.Reduced].Should().Be(100m);        // 1000 * 10%
        result[IvaRate.Intermediate].Should().Be(50m);    // 1000 * 5%
        result[IvaRate.SuperReduced].Should().Be(40m);    // 1000 * 4%
        result[IvaRate.Zero].Should().Be(0m);             // 1000 * 0%
    }

    #endregion

    #region Regime Forfettario Tests

    [Fact]
    public void CalculateInvoiceTotals_Forfettario_IvaIsZero()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = true,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        _bolloServiceMock.CalculateBollo(invoice).Returns(2.00m);

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.IvaTotal.Should().Be(0m, "IVA must be 0 for Regime Forfettario per Legge 190/2014");
        invoice.Items[0].IvaAmount.Should().Be(0m);
    }

    [Fact]
    public void CalculateInvoiceTotals_Forfettario_RitenutaIsZero()
    {
        // Arrange
        var client = new Client
        {
            SubjectToRitenuta = true,
            RitenutaPercentage = 20m
        };

        var invoice = new Invoice
        {
            IsRegimeForfettario = true,
            Client = client,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        _bolloServiceMock.CalculateBollo(invoice).Returns(2.00m);

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.RitenutaAmount.Should().Be(0m, "Ritenuta must be 0 for Regime Forfettario per Legge 190/2014");
    }

    [Fact]
    public void CalculateInvoiceTotals_Forfettario_Over7747_BolloApplied()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = true,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        _bolloServiceMock.CalculateBollo(invoice).Returns(2.00m);

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.BolloAmount.Should().Be(2.00m, "Bollo required when imponibile > 77.47 EUR per DPR 642/72 Art. 13");
        _bolloServiceMock.Received(1).CalculateBollo(invoice);
    }

    [Fact]
    public void CalculateInvoiceTotals_Forfettario_Under7747_NoBolloApplied()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = true,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 50, IvaRate = IvaRate.Standard }
            }
        };

        _bolloServiceMock.CalculateBollo(invoice).Returns(0m);

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.BolloAmount.Should().Be(0m, "No bollo required when imponibile <= 77.47 EUR");
        _bolloServiceMock.Received(1).CalculateBollo(invoice);
    }

    [Fact]
    public void CalculateInvoiceTotals_Forfettario_TotalDue_EqualsImponibilePlusBollo()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = true,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        _bolloServiceMock.CalculateBollo(invoice).Returns(2.00m);

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.ImponibileTotal.Should().Be(1000m);
        invoice.IvaTotal.Should().Be(0m);
        invoice.RitenutaAmount.Should().Be(0m);
        invoice.BolloAmount.Should().Be(2.00m);
        invoice.TotalDue.Should().Be(1002.00m, "TotalDue = Imponibile + Bollo for Regime Forfettario");
    }

    [Fact]
    public void CalculateInvoiceTotals_Forfettario_IvaByRateIsEmpty()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = true,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        _bolloServiceMock.CalculateBollo(invoice).Returns(2.00m);

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.IvaByRate.Should().BeEmpty("No IVA breakdown for Regime Forfettario");
    }

    [Fact]
    public void CalculateInvoiceTotals_NonForfettario_NoBolloApplied()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = false,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.BolloAmount.Should().Be(0m, "No bollo for standard invoices with IVA");
        _bolloServiceMock.DidNotReceive().CalculateBollo(Arg.Any<Invoice>());
    }

    #endregion

    #region Split Payment Tests

    [Fact]
    public void CalculateInvoiceTotals_SplitPayment_TotalDueExcludesIva()
    {
        // Art. 17-ter DPR 633/72: PA pays only imponibile, IVA goes to Treasury
        // Arrange
        var client = new Client
        {
            ClientType = ClientType.PublicAdministration,
            SubjectToSplitPayment = true,
            SubjectToRitenuta = false
        };

        var invoice = new Invoice
        {
            Client = client,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 1, UnitPrice = 1000, IvaRate = IvaRate.Standard }
            }
        };

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        invoice.ImponibileTotal.Should().Be(1000m);
        invoice.IvaTotal.Should().Be(220m);
        invoice.SubTotal.Should().Be(1220m);
        invoice.TotalDue.Should().Be(1000m, "PA pays only imponibile with split payment");
        invoice.RitenutaAmount.Should().Be(0m, "Ritenuta not applicable with split payment");
    }

    [Fact]
    public void CalculateInvoiceTotals_SplitPayment_RitenutaIsZero()
    {
        // Split payment and ritenuta are mutually exclusive
        // Arrange
        var client = new Client
        {
            ClientType = ClientType.PublicAdministration,
            SubjectToSplitPayment = true,
            SubjectToRitenuta = true, // Even if set to true, split payment takes precedence
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

        // Assert
        invoice.RitenutaAmount.Should().Be(0m, "Split payment and ritenuta are mutually exclusive per Art. 17-ter DPR 633/72");
        invoice.TotalDue.Should().Be(1000m);
        _ritenutaServiceMock.DidNotReceive().CalculateRitenuta(Arg.Any<decimal>(), Arg.Any<decimal>());
    }

    [Fact]
    public void CalculateInvoiceTotals_SplitPayment_MultipleItems_CalculatesCorrectly()
    {
        // Arrange
        var client = new Client
        {
            ClientType = ClientType.PublicAdministration,
            SubjectToSplitPayment = true,
            SubjectToRitenuta = false
        };

        var invoice = new Invoice
        {
            Client = client,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Quantity = 10, UnitPrice = 100, IvaRate = IvaRate.Standard },
                new InvoiceItem { Quantity = 5, UnitPrice = 200, IvaRate = IvaRate.Reduced }
            }
        };

        // Act
        _sut.CalculateInvoiceTotals(invoice);

        // Assert
        // Imponibile: 1000 + 1000 = 2000
        // IVA: 220 + 100 = 320
        // SubTotal: 2320
        // TotalDue: 2000 (split payment, IVA excluded)
        invoice.ImponibileTotal.Should().Be(2000m);
        invoice.IvaTotal.Should().Be(320m);
        invoice.SubTotal.Should().Be(2320m);
        invoice.TotalDue.Should().Be(2000m);
    }

    [Fact]
    public void CalculateInvoiceTotals_NotSplitPayment_TotalDueIncludesIva()
    {
        // Verify non-split-payment invoices still work correctly
        // Arrange
        var client = new Client
        {
            SubjectToSplitPayment = false,
            SubjectToRitenuta = false
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
        invoice.TotalDue.Should().Be(1220m, "Non-split-payment invoices include IVA in TotalDue");
    }

    #endregion
}
