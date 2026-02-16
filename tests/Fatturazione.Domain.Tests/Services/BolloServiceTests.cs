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

    #region Extended Bollo - NaturaIva Tests (DPR 642/72 Art. 13)

    [Fact]
    public void RequiresBollo_NonForfettario_N4ExemptItems_Over7747_ReturnsTrue()
    {
        // Art. 13 DPR 642/72: bollo applies when non-IVA portion exceeds 77.47 EUR
        // N4 = Esenti (art. 10 DPR 633/72)

        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = false,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    IvaRate = IvaRate.Zero,
                    NaturaIva = NaturaIva.N4,
                    Imponibile = 100.00m
                }
            }
        };

        // Act
        var result = _sut.RequiresBollo(invoice);

        // Assert
        result.Should().BeTrue("N4 exempt items over 77.47 EUR require bollo per DPR 642/72 Art. 13");
    }

    [Fact]
    public void RequiresBollo_NonForfettario_N4ExemptItems_Under7747_ReturnsFalse()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = false,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Quantity = 1,
                    UnitPrice = 50.00m,
                    IvaRate = IvaRate.Zero,
                    NaturaIva = NaturaIva.N4,
                    Imponibile = 50.00m
                }
            }
        };

        // Act
        var result = _sut.RequiresBollo(invoice);

        // Assert
        result.Should().BeFalse("N4 exempt items at 50 EUR do not exceed 77.47 EUR threshold");
    }

    [Fact]
    public void RequiresBollo_NonForfettario_MixedInvoice_N1PortionOver7747_ReturnsTrue()
    {
        // Mixed invoice: IVA items + N1 items. Only N1 portion counts toward bollo threshold.
        // N1 = Escluse ex art. 15 DPR 633/72

        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = false,
            Items = new List<InvoiceItem>
            {
                // Standard IVA item — does NOT count toward bollo threshold
                new InvoiceItem
                {
                    Quantity = 1,
                    UnitPrice = 500.00m,
                    IvaRate = IvaRate.Standard,
                    NaturaIva = null,
                    Imponibile = 500.00m
                },
                // N1 excluded item — counts toward bollo threshold
                new InvoiceItem
                {
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    IvaRate = IvaRate.Zero,
                    NaturaIva = NaturaIva.N1,
                    Imponibile = 100.00m
                }
            }
        };

        // Act
        var result = _sut.RequiresBollo(invoice);

        // Assert
        result.Should().BeTrue("N1 portion (100 EUR) exceeds 77.47 EUR threshold");
    }

    [Fact]
    public void RequiresBollo_NonForfettario_MixedInvoice_N1PortionUnder7747_ReturnsFalse()
    {
        // Mixed invoice: IVA items + N1 items. N1 portion under threshold.

        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = false,
            Items = new List<InvoiceItem>
            {
                // Standard IVA item — does NOT count toward bollo threshold
                new InvoiceItem
                {
                    Quantity = 1,
                    UnitPrice = 500.00m,
                    IvaRate = IvaRate.Standard,
                    NaturaIva = null,
                    Imponibile = 500.00m
                },
                // N1 excluded item — counts toward bollo threshold but under 77.47
                new InvoiceItem
                {
                    Quantity = 1,
                    UnitPrice = 50.00m,
                    IvaRate = IvaRate.Zero,
                    NaturaIva = NaturaIva.N1,
                    Imponibile = 50.00m
                }
            }
        };

        // Act
        var result = _sut.RequiresBollo(invoice);

        // Assert
        result.Should().BeFalse("N1 portion (50 EUR) does not exceed 77.47 EUR threshold");
    }

    [Fact]
    public void CalculateBollo_NonForfettario_N4ExemptOver7747_Returns200()
    {
        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = false,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    IvaRate = IvaRate.Zero,
                    NaturaIva = NaturaIva.N4,
                    Imponibile = 100.00m
                }
            }
        };

        // Act
        var result = _sut.CalculateBollo(invoice);

        // Assert
        result.Should().Be(2.00m, "Bollo is 2.00 EUR when N4 exempt items exceed 77.47 EUR");
    }

    [Theory]
    [InlineData(NaturaIva.N1)]
    [InlineData(NaturaIva.N2_1)]
    [InlineData(NaturaIva.N2_2)]
    [InlineData(NaturaIva.N3_5)]
    [InlineData(NaturaIva.N3_6)]
    [InlineData(NaturaIva.N4)]
    public void RequiresBollo_NonForfettario_AllBolloNaturaIvaCodes_Over7747_ReturnsTrue(NaturaIva naturaIva)
    {
        // All NaturaIva codes subject to bollo per DPR 642/72 Art. 13

        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = false,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    IvaRate = IvaRate.Zero,
                    NaturaIva = naturaIva,
                    Imponibile = 100.00m
                }
            }
        };

        // Act
        var result = _sut.RequiresBollo(invoice);

        // Assert
        result.Should().BeTrue($"NaturaIva {naturaIva} items over 77.47 EUR require bollo");
    }

    [Theory]
    [InlineData(NaturaIva.N3_1)]
    [InlineData(NaturaIva.N3_2)]
    [InlineData(NaturaIva.N3_3)]
    [InlineData(NaturaIva.N3_4)]
    [InlineData(NaturaIva.N5)]
    [InlineData(NaturaIva.N6_1)]
    [InlineData(NaturaIva.N7)]
    public void RequiresBollo_NonForfettario_NonBolloNaturaIvaCodes_Over7747_ReturnsFalse(NaturaIva naturaIva)
    {
        // NaturaIva codes NOT subject to bollo (export operations, reverse charge, etc.)

        // Arrange
        var invoice = new Invoice
        {
            IsRegimeForfettario = false,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    IvaRate = IvaRate.Zero,
                    NaturaIva = naturaIva,
                    Imponibile = 100.00m
                }
            }
        };

        // Act
        var result = _sut.RequiresBollo(invoice);

        // Assert
        result.Should().BeFalse($"NaturaIva {naturaIva} items do not require bollo");
    }

    #endregion
}
