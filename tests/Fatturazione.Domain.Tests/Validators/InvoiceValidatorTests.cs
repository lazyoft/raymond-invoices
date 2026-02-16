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
                new InvoiceItem { Description = "Service", Quantity = 1, UnitPrice = 100, IvaRate = IvaRate.Standard }
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
                new InvoiceItem { Description = "", IvaRate = IvaRate.Standard }
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
                new InvoiceItem { Description = "", IvaRate = IvaRate.Standard }
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
                new InvoiceItem { Description = "Service", Quantity = 0, UnitPrice = 100, IvaRate = IvaRate.Standard }
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
                new InvoiceItem { Description = "Service", Quantity = -1, UnitPrice = 100, IvaRate = IvaRate.Standard }
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
                new InvoiceItem { Description = "Service", Quantity = 1, UnitPrice = -50, IvaRate = IvaRate.Standard }
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
                new InvoiceItem { Description = "Free Sample", Quantity = 1, UnitPrice = 0, IvaRate = IvaRate.Standard }
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
    }

    #endregion

    #region Gap 10.2 - Data fattura non nel futuro

    [Fact]
    public void Validate_WithFutureInvoiceDate_ReturnsError()
    {
        // Data fattura domani → errore
        var invoice = CreateValidInvoice();
        invoice.InvoiceDate = DateTime.Today.AddDays(1);
        invoice.DueDate = DateTime.Today.AddDays(31);

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain("InvoiceDate non può essere nel futuro");
    }

    [Fact]
    public void Validate_WithTodayInvoiceDate_IsValid()
    {
        // Data fattura oggi → OK
        var invoice = CreateValidInvoice();
        invoice.InvoiceDate = DateTime.Today;
        invoice.DueDate = DateTime.Today.AddDays(30);

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithYesterdayInvoiceDate_IsValid()
    {
        // Data fattura ieri → OK
        var invoice = CreateValidInvoice();
        invoice.InvoiceDate = DateTime.Today.AddDays(-1);
        invoice.DueDate = DateTime.Today.AddDays(29);

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region Gap 10.2 - NaturaIva validation

    [Fact]
    public void Validate_WithIvaRateZeroAndNaturaIvaSet_IsValid()
    {
        // IvaRate.Zero + NaturaIva.N4 → OK
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Prestazione esente",
                Quantity = 1,
                UnitPrice = 100,
                IvaRate = IvaRate.Zero,
                NaturaIva = NaturaIva.N4
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithIvaRateZeroAndNaturaIvaNull_ReturnsError()
    {
        // IvaRate.Zero + NaturaIva = null → errore
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Prestazione",
                Quantity = 1,
                UnitPrice = 100,
                IvaRate = IvaRate.Zero,
                NaturaIva = null
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain("Item 1: NaturaIva è obbligatoria quando IvaRate è Zero (Specifiche FatturaPA, campo 2.2.1.14)");
    }

    [Fact]
    public void Validate_WithIvaRateStandardAndNaturaIvaSet_ReturnsError()
    {
        // IvaRate.Standard + NaturaIva.N4 → errore
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Prestazione standard",
                Quantity = 1,
                UnitPrice = 100,
                IvaRate = IvaRate.Standard,
                NaturaIva = NaturaIva.N4
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain("Item 1: NaturaIva deve essere null quando IvaRate non è Zero");
    }

    [Fact]
    public void Validate_WithIvaRateStandardAndNaturaIvaNull_IsValid()
    {
        // IvaRate.Standard + NaturaIva = null → OK
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Servizio",
                Quantity = 1,
                UnitPrice = 100,
                IvaRate = IvaRate.Standard,
                NaturaIva = null
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region Gap 11 - Forfettario Causale

    [Fact]
    public void Validate_Forfettario_WithoutCausale_ReturnsError()
    {
        // Forfettario senza Causale → errore
        var invoice = CreateValidInvoice();
        invoice.IsRegimeForfettario = true;
        invoice.Causale = null;

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("regime forfettario") && e.Contains("Causale"));
    }

    [Fact]
    public void Validate_Forfettario_WithEmptyCausale_ReturnsError()
    {
        // Forfettario con Causale vuota → errore
        var invoice = CreateValidInvoice();
        invoice.IsRegimeForfettario = true;
        invoice.Causale = "";

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("regime forfettario") && e.Contains("Causale"));
    }

    [Fact]
    public void Validate_Forfettario_WithCorrectCausale_IsValid()
    {
        // Forfettario con Causale corretta → OK
        var invoice = CreateValidInvoice();
        invoice.IsRegimeForfettario = true;
        invoice.Causale = "Operazione effettuata ai sensi dell'art. 1, commi 54-89, Legge n. 190/2014";

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Forfettario_WithCorrectCausaleCaseInsensitive_IsValid()
    {
        // Forfettario con Causale corretta (case diverso) → OK
        var invoice = CreateValidInvoice();
        invoice.IsRegimeForfettario = true;
        invoice.Causale = "OPERAZIONE EFFETTUATA AI SENSI DELL'ART. 1, COMMI 54-89, LEGGE N. 190/2014";

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Forfettario_WithWrongCausaleText_ReturnsError()
    {
        // Forfettario con testo sbagliato → errore
        var invoice = CreateValidInvoice();
        invoice.IsRegimeForfettario = true;
        invoice.Causale = "Compenso per consulenza informatica";

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("regime forfettario") && e.Contains("art. 1, commi 54-89"));
    }

    [Fact]
    public void Validate_Forfettario_WithPartialCausale_MissingLawRef_ReturnsError()
    {
        // Forfettario con solo articolo ma senza legge → errore
        var invoice = CreateValidInvoice();
        invoice.IsRegimeForfettario = true;
        invoice.Causale = "Operazione ai sensi dell'art. 1, commi 54-89";

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("regime forfettario"));
    }

    [Fact]
    public void Validate_NonForfettario_WithoutCausale_IsValid()
    {
        // Non forfettario senza Causale → OK (non è obbligatoria)
        var invoice = CreateValidInvoice();
        invoice.IsRegimeForfettario = false;
        invoice.Causale = null;

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region Gap 2.3 - Reverse Charge

    [Fact]
    public void Validate_ReverseCharge_N6_1_WithIvaRateZero_IsValid()
    {
        // N6_1 + IvaRate.Zero → OK (reverse charge corretto)
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Cessione rottami",
                Quantity = 1,
                UnitPrice = 500,
                IvaRate = IvaRate.Zero,
                NaturaIva = NaturaIva.N6_1
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReverseCharge_N6_3_WithIvaRateZero_IsValid()
    {
        // N6_3 (subappalto edilizia) + IvaRate.Zero → OK
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Subappalto edilizia",
                Quantity = 1,
                UnitPrice = 1000,
                IvaRate = IvaRate.Zero,
                NaturaIva = NaturaIva.N6_3
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReverseCharge_N6_1_WithIvaRateStandard_ReturnsError()
    {
        // N6_1 + IvaRate.Standard → errore (reverse charge richiede Zero)
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Cessione rottami",
                Quantity = 1,
                UnitPrice = 500,
                IvaRate = IvaRate.Standard,
                NaturaIva = NaturaIva.N6_1
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        // Should have both errors: NaturaIva must be null when IvaRate != Zero, AND reverse charge requires Zero
        errors.Should().Contain(e => e.Contains("NaturaIva deve essere null quando IvaRate non è Zero"));
        errors.Should().Contain(e => e.Contains("reverse charge") && e.Contains("Zero"));
    }

    [Fact]
    public void Validate_ReverseCharge_N6_9_WithIvaRateZero_IsValid()
    {
        // N6_9 (altri casi) + IvaRate.Zero → OK
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Operazione in reverse charge",
                Quantity = 1,
                UnitPrice = 200,
                IvaRate = IvaRate.Zero,
                NaturaIva = NaturaIva.N6_9
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region Gap 1.3 - Simplified Invoice

    [Fact]
    public void Validate_Simplified_Forfettario_Over400_IsValid()
    {
        // Semplificata + forfettario + 500 EUR → OK (nessun limite dal 01/01/2025)
        var invoice = CreateValidInvoice();
        invoice.IsSimplified = true;
        invoice.IsRegimeForfettario = true;
        invoice.Causale = "Operazione effettuata ai sensi dell'art. 1, commi 54-89, Legge n. 190/2014";
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Consulenza",
                Quantity = 1,
                UnitPrice = 500,
                IvaRate = IvaRate.Zero,
                NaturaIva = NaturaIva.N2_2
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Simplified_NonForfettario_Over400_ReturnsError()
    {
        // Semplificata + non forfettario + 500 EUR → errore (limite 400 EUR)
        var invoice = CreateValidInvoice();
        invoice.IsSimplified = true;
        invoice.IsRegimeForfettario = false;
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Consulenza",
                Quantity = 1,
                UnitPrice = 500,
                IvaRate = IvaRate.Standard
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("400 EUR") && e.Contains("semplificata"));
    }

    [Fact]
    public void Validate_Simplified_NonForfettario_Under400_IsValid()
    {
        // Semplificata + non forfettario + 300 EUR → OK
        var invoice = CreateValidInvoice();
        invoice.IsSimplified = true;
        invoice.IsRegimeForfettario = false;
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Servizio piccolo",
                Quantity = 1,
                UnitPrice = 300,
                IvaRate = IvaRate.Standard
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Simplified_NonForfettario_Exactly400_IsValid()
    {
        // Semplificata + non forfettario + esattamente 400 EUR → OK (limite è "supera", non "uguale")
        var invoice = CreateValidInvoice();
        invoice.IsSimplified = true;
        invoice.IsRegimeForfettario = false;
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Servizio",
                Quantity = 1,
                UnitPrice = 400,
                IvaRate = IvaRate.Standard
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Simplified_NonForfettario_MultipleItemsOver400_ReturnsError()
    {
        // Semplificata + non forfettario + multiple items totale > 400 EUR → errore
        var invoice = CreateValidInvoice();
        invoice.IsSimplified = true;
        invoice.IsRegimeForfettario = false;
        invoice.Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Servizio A",
                Quantity = 2,
                UnitPrice = 150,
                IvaRate = IvaRate.Standard
            },
            new InvoiceItem
            {
                Description = "Servizio B",
                Quantity = 1,
                UnitPrice = 200,
                IvaRate = IvaRate.Standard
            }
        };

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("400 EUR"));
    }

    #endregion

    #region Gap 13 - Emission Deadlines

    [Fact]
    public void Validate_TD01_DataOperazione15DaysAgo_ReturnsError()
    {
        // TD01 (immediata) con DataOperazione 15 giorni fa → errore (max 12 giorni)
        var invoice = CreateValidInvoice();
        invoice.DocumentType = DocumentType.TD01;
        invoice.DataOperazione = DateTime.Today.AddDays(-15);
        invoice.InvoiceDate = DateTime.Today;
        invoice.DueDate = DateTime.Today.AddDays(30);

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("TD01") && e.Contains("12 giorni"));
    }

    [Fact]
    public void Validate_TD01_DataOperazione10DaysAgo_IsValid()
    {
        // TD01 (immediata) con DataOperazione 10 giorni fa → OK (entro 12 giorni)
        var invoice = CreateValidInvoice();
        invoice.DocumentType = DocumentType.TD01;
        invoice.DataOperazione = DateTime.Today.AddDays(-10);
        invoice.InvoiceDate = DateTime.Today;
        invoice.DueDate = DateTime.Today.AddDays(30);

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_TD01_DataOperazione12DaysAgo_IsValid()
    {
        // TD01 (immediata) con DataOperazione esattamente 12 giorni fa → OK (limite incluso)
        var invoice = CreateValidInvoice();
        invoice.DocumentType = DocumentType.TD01;
        invoice.DataOperazione = DateTime.Today.AddDays(-12);
        invoice.InvoiceDate = DateTime.Today;
        invoice.DueDate = DateTime.Today.AddDays(30);

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_TD01_DataOperazione13DaysAgo_ReturnsError()
    {
        // TD01 (immediata) con DataOperazione 13 giorni fa → errore (supera 12 giorni)
        var invoice = CreateValidInvoice();
        invoice.DocumentType = DocumentType.TD01;
        invoice.DataOperazione = DateTime.Today.AddDays(-13);
        invoice.InvoiceDate = DateTime.Today;
        invoice.DueDate = DateTime.Today.AddDays(30);

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("TD01") && e.Contains("12 giorni"));
    }

    [Fact]
    public void Validate_TD24_InvoiceDate14thOfNextMonth_IsValid()
    {
        // TD24 (differita) con DataOperazione del mese precedente, data fattura = 14 del mese successivo → OK
        var dataOperazione = new DateTime(2026, 1, 20);
        var invoiceDate = new DateTime(2026, 2, 14);

        var invoice = CreateValidInvoice();
        invoice.DocumentType = DocumentType.TD24;
        invoice.DataOperazione = dataOperazione;
        invoice.InvoiceDate = invoiceDate;
        invoice.DueDate = invoiceDate.AddDays(30);

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_TD24_InvoiceDate15thOfNextMonth_IsValid()
    {
        // TD24 (differita) con DataOperazione del mese precedente, data fattura = 15 del mese successivo → OK (limite incluso)
        var dataOperazione = new DateTime(2026, 1, 20);
        var invoiceDate = new DateTime(2026, 2, 15);

        var invoice = CreateValidInvoice();
        invoice.DocumentType = DocumentType.TD24;
        invoice.DataOperazione = dataOperazione;
        invoice.InvoiceDate = invoiceDate;
        invoice.DueDate = invoiceDate.AddDays(30);

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_TD24_InvoiceDate16thOfNextMonth_ReturnsError()
    {
        // TD24 (differita) con DataOperazione del mese precedente, data fattura = 16 del mese successivo → errore
        var dataOperazione = new DateTime(2026, 1, 20);
        var invoiceDate = new DateTime(2026, 2, 16);

        var invoice = CreateValidInvoice();
        invoice.DocumentType = DocumentType.TD24;
        invoice.DataOperazione = dataOperazione;
        invoice.InvoiceDate = invoiceDate;
        invoice.DueDate = invoiceDate.AddDays(30);

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("TD24") && e.Contains("15 del mese successivo"));
    }

    [Fact]
    public void Validate_TD24_DecemberOperation_InvoiceJanuary15_IsValid()
    {
        // TD24 con operazione a dicembre, fattura emessa il 15 gennaio dell'anno successivo → OK
        var dataOperazione = new DateTime(2025, 12, 10);
        var invoiceDate = new DateTime(2026, 1, 15);

        var invoice = CreateValidInvoice();
        invoice.DocumentType = DocumentType.TD24;
        invoice.DataOperazione = dataOperazione;
        invoice.InvoiceDate = invoiceDate;
        invoice.DueDate = invoiceDate.AddDays(30);

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithoutDataOperazione_SkipsDeadlineCheck()
    {
        // Senza DataOperazione, non si applicano controlli sui termini
        var invoice = CreateValidInvoice();
        invoice.DocumentType = DocumentType.TD01;
        invoice.DataOperazione = null;

        var (isValid, errors) = InvoiceValidator.Validate(invoice);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a valid base invoice for testing — all required fields set correctly
    /// </summary>
    private static Invoice CreateValidInvoice()
    {
        return new Invoice
        {
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Description = "Servizio di consulenza",
                    Quantity = 1,
                    UnitPrice = 100,
                    IvaRate = IvaRate.Standard
                }
            }
        };
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
