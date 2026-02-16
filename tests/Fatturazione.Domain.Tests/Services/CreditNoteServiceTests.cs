using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Services;

/// <summary>
/// Tests for CreditNoteService — Note di Credito (TD04) e Note di Debito (TD05)
/// Art. 26 DPR 633/72
/// </summary>
public class CreditNoteServiceTests
{
    private readonly CreditNoteService _sut;

    public CreditNoteServiceTests()
    {
        _sut = new CreditNoteService();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a standard issued invoice for testing.
    /// Imponibile: 1000 EUR, IVA 22%: 220 EUR, SubTotal: 1220 EUR, TotalDue: 1220 EUR
    /// </summary>
    private static Invoice CreateIssuedInvoice()
    {
        var invoiceId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        return new Invoice
        {
            Id = invoiceId,
            InvoiceNumber = "2026/001",
            InvoiceDate = new DateTime(2026, 1, 15),
            DueDate = new DateTime(2026, 2, 15),
            ClientId = clientId,
            Client = new Client
            {
                Id = clientId,
                RagioneSociale = "Acme SRL",
                PartitaIva = "01234567890",
                ClientType = ClientType.Company
            },
            DocumentType = DocumentType.TD01,
            Status = InvoiceStatus.Issued,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Consulenza informatica",
                    Quantity = 10,
                    UnitPrice = 100m,
                    IvaRate = IvaRate.Standard,
                    Imponibile = 1000m,
                    IvaAmount = 220m,
                    Total = 1220m
                }
            },
            ImponibileTotal = 1000m,
            IvaTotal = 220m,
            SubTotal = 1220m,
            RitenutaAmount = 0m,
            BolloAmount = 0m,
            TotalDue = 1220m
        };
    }

    /// <summary>
    /// Creates an issued invoice with ritenuta d'acconto (professional client).
    /// Imponibile: 1000 EUR, IVA 22%: 220 EUR, SubTotal: 1220 EUR,
    /// Ritenuta 20% of 1000: 200 EUR, TotalDue: 1020 EUR
    /// </summary>
    private static Invoice CreateIssuedInvoiceWithRitenuta()
    {
        var invoiceId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        return new Invoice
        {
            Id = invoiceId,
            InvoiceNumber = "2026/002",
            InvoiceDate = new DateTime(2026, 1, 20),
            DueDate = new DateTime(2026, 2, 20),
            ClientId = clientId,
            Client = new Client
            {
                Id = clientId,
                RagioneSociale = "Dott. Mario Rossi",
                PartitaIva = "09876543210",
                ClientType = ClientType.Professional,
                SubjectToRitenuta = true,
                RitenutaPercentage = 20m
            },
            DocumentType = DocumentType.TD01,
            Status = InvoiceStatus.Issued,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Prestazione professionale",
                    Quantity = 1,
                    UnitPrice = 1000m,
                    IvaRate = IvaRate.Standard,
                    Imponibile = 1000m,
                    IvaAmount = 220m,
                    Total = 1220m
                }
            },
            ImponibileTotal = 1000m,
            IvaTotal = 220m,
            SubTotal = 1220m,
            RitenutaAmount = 200m,
            BolloAmount = 0m,
            TotalDue = 1020m
        };
    }

    /// <summary>
    /// Creates a draft invoice (not yet issued, no fiscal value).
    /// </summary>
    private static Invoice CreateDraftInvoice()
    {
        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = string.Empty,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            ClientId = Guid.NewGuid(),
            DocumentType = DocumentType.TD01,
            Status = InvoiceStatus.Draft,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Servizio",
                    Quantity = 1,
                    UnitPrice = 500m,
                    IvaRate = IvaRate.Standard,
                    Imponibile = 500m,
                    IvaAmount = 110m,
                    Total = 610m
                }
            },
            ImponibileTotal = 500m,
            IvaTotal = 110m,
            SubTotal = 610m,
            TotalDue = 610m
        };
    }

    /// <summary>
    /// Creates an issued invoice with multiple items at different IVA rates.
    /// Item 1: 800 EUR @ 22% IVA = 176 EUR
    /// Item 2: 200 EUR @ 10% IVA = 20 EUR
    /// Total Imponibile: 1000, IVA: 196, SubTotal: 1196, TotalDue: 1196
    /// </summary>
    private static Invoice CreateIssuedInvoiceWithMultipleItems()
    {
        var invoiceId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        return new Invoice
        {
            Id = invoiceId,
            InvoiceNumber = "2026/003",
            InvoiceDate = new DateTime(2026, 2, 1),
            DueDate = new DateTime(2026, 3, 1),
            ClientId = clientId,
            Client = new Client
            {
                Id = clientId,
                RagioneSociale = "Beta SPA",
                PartitaIva = "11223344556",
                ClientType = ClientType.Company
            },
            DocumentType = DocumentType.TD01,
            Status = InvoiceStatus.Sent,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Sviluppo software",
                    Quantity = 8,
                    UnitPrice = 100m,
                    IvaRate = IvaRate.Standard,
                    Imponibile = 800m,
                    IvaAmount = 176m,
                    Total = 976m
                },
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Manutenzione",
                    Quantity = 2,
                    UnitPrice = 100m,
                    IvaRate = IvaRate.Reduced,
                    Imponibile = 200m,
                    IvaAmount = 20m,
                    Total = 220m
                }
            },
            ImponibileTotal = 1000m,
            IvaTotal = 196m,
            SubTotal = 1196m,
            RitenutaAmount = 0m,
            TotalDue = 1196m
        };
    }

    #endregion

    #region CreateCreditNote Tests

    [Fact]
    public void CreateCreditNote_FromIssuedInvoice_SetsDocumentTypeTD04()
    {
        // Arrange
        var original = CreateIssuedInvoice();

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Annullamento fattura");

        // Assert
        creditNote.DocumentType.Should().Be(DocumentType.TD04);
    }

    [Fact]
    public void CreateCreditNote_FromIssuedInvoice_SetsStatusToDraft()
    {
        // Arrange
        var original = CreateIssuedInvoice();

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Annullamento fattura");

        // Assert
        creditNote.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void CreateCreditNote_FromIssuedInvoice_SetsRelatedInvoiceId()
    {
        // Arrange
        var original = CreateIssuedInvoice();

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Rettifica inesattezze");

        // Assert
        creditNote.RelatedInvoiceId.Should().Be(original.Id);
    }

    [Fact]
    public void CreateCreditNote_FromIssuedInvoice_SetsRelatedInvoiceNumber()
    {
        // Arrange
        var original = CreateIssuedInvoice();

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Rettifica inesattezze");

        // Assert
        creditNote.RelatedInvoiceNumber.Should().Be("2026/001");
    }

    [Fact]
    public void CreateCreditNote_FromIssuedInvoice_CopiesItemsWithNegatedAmounts()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        // Original: Quantity=10, UnitPrice=100, Imponibile=1000, IvaAmount=220, Total=1220

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Reso merce");

        // Assert
        creditNote.Items.Should().HaveCount(1);

        var creditItem = creditNote.Items[0];
        creditItem.Description.Should().Be("Consulenza informatica");
        creditItem.Quantity.Should().Be(10);
        creditItem.UnitPrice.Should().Be(-100m);
        creditItem.Imponibile.Should().Be(-1000m);
        creditItem.IvaAmount.Should().Be(-220m);
        creditItem.Total.Should().Be(-1220m);
        creditItem.IvaRate.Should().Be(IvaRate.Standard);
    }

    [Fact]
    public void CreateCreditNote_FromIssuedInvoice_NegatesAllTotals()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        // Original: ImponibileTotal=1000, IvaTotal=220, SubTotal=1220, TotalDue=1220

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Annullamento");

        // Assert
        creditNote.ImponibileTotal.Should().Be(-1000m);
        creditNote.IvaTotal.Should().Be(-220m);
        creditNote.SubTotal.Should().Be(-1220m);
        creditNote.TotalDue.Should().Be(-1220m);
    }

    [Fact]
    public void CreateCreditNote_FromInvoiceWithRitenuta_NegatesRitenutaAmount()
    {
        // Arrange
        var original = CreateIssuedInvoiceWithRitenuta();
        // Original: Ritenuta=200, TotalDue=1020

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Errore importo");

        // Assert
        creditNote.RitenutaAmount.Should().Be(-200m);
        creditNote.TotalDue.Should().Be(-1020m);
    }

    [Fact]
    public void CreateCreditNote_FromInvoiceWithMultipleItems_CopiesAllItemsNegated()
    {
        // Arrange
        var original = CreateIssuedInvoiceWithMultipleItems();

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Reso totale");

        // Assert
        creditNote.Items.Should().HaveCount(2);

        creditNote.Items[0].Description.Should().Be("Sviluppo software");
        creditNote.Items[0].Imponibile.Should().Be(-800m);
        creditNote.Items[0].IvaAmount.Should().Be(-176m);

        creditNote.Items[1].Description.Should().Be("Manutenzione");
        creditNote.Items[1].Imponibile.Should().Be(-200m);
        creditNote.Items[1].IvaAmount.Should().Be(-20m);

        creditNote.ImponibileTotal.Should().Be(-1000m);
        creditNote.IvaTotal.Should().Be(-196m);
    }

    [Fact]
    public void CreateCreditNote_SetsReasonInNotesAndCausale()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var reason = "Abbuono contrattuale come da accordo del 15/01/2026";

        // Act
        var creditNote = _sut.CreateCreditNote(original, reason);

        // Assert
        creditNote.Notes.Should().Be(reason);
        creditNote.Causale.Should().Be(reason);
    }

    [Fact]
    public void CreateCreditNote_PreservesClientReference()
    {
        // Arrange
        var original = CreateIssuedInvoice();

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Annullamento");

        // Assert
        creditNote.ClientId.Should().Be(original.ClientId);
        creditNote.Client.Should().BeSameAs(original.Client);
    }

    [Fact]
    public void CreateCreditNote_GeneratesNewId()
    {
        // Arrange
        var original = CreateIssuedInvoice();

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Annullamento");

        // Assert
        creditNote.Id.Should().NotBe(Guid.Empty);
        creditNote.Id.Should().NotBe(original.Id);
    }

    [Fact]
    public void CreateCreditNote_SetsBolloAmountToZero()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        original.BolloAmount = 2.00m;
        original.BolloVirtuale = true;

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Annullamento");

        // Assert — Bollo is not duplicated on credit notes
        creditNote.BolloAmount.Should().Be(0m);
    }

    [Fact]
    public void CreateCreditNote_PreservesRegimeForfettarioFlag()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        original.IsRegimeForfettario = true;

        // Act
        var creditNote = _sut.CreateCreditNote(original, "Rettifica");

        // Assert
        creditNote.IsRegimeForfettario.Should().BeTrue();
    }

    #endregion

    #region CreateDebitNote Tests

    [Fact]
    public void CreateDebitNote_SetsDocumentTypeTD05()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var additionalItems = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Servizio aggiuntivo",
                Quantity = 1,
                UnitPrice = 200m,
                IvaRate = IvaRate.Standard,
                Imponibile = 200m,
                IvaAmount = 44m,
                Total = 244m
            }
        };

        // Act
        var debitNote = _sut.CreateDebitNote(original, additionalItems, "Integrazione corrispettivo");

        // Assert
        debitNote.DocumentType.Should().Be(DocumentType.TD05);
    }

    [Fact]
    public void CreateDebitNote_SetsStatusToDraft()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var additionalItems = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Supplemento",
                Quantity = 1,
                UnitPrice = 100m,
                IvaRate = IvaRate.Standard,
                Imponibile = 100m,
                IvaAmount = 22m,
                Total = 122m
            }
        };

        // Act
        var debitNote = _sut.CreateDebitNote(original, additionalItems, "Variazione in aumento");

        // Assert
        debitNote.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void CreateDebitNote_SetsRelatedInvoiceReferences()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var additionalItems = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Ore aggiuntive",
                Quantity = 5,
                UnitPrice = 100m,
                IvaRate = IvaRate.Standard,
                Imponibile = 500m,
                IvaAmount = 110m,
                Total = 610m
            }
        };

        // Act
        var debitNote = _sut.CreateDebitNote(original, additionalItems, "Ore non fatturate");

        // Assert
        debitNote.RelatedInvoiceId.Should().Be(original.Id);
        debitNote.RelatedInvoiceNumber.Should().Be("2026/001");
    }

    [Fact]
    public void CreateDebitNote_ContainsOnlyAdditionalItems()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var additionalItems = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Maggiorazione urgenza",
                Quantity = 1,
                UnitPrice = 300m,
                IvaRate = IvaRate.Standard,
                Imponibile = 300m,
                IvaAmount = 66m,
                Total = 366m
            }
        };

        // Act
        var debitNote = _sut.CreateDebitNote(original, additionalItems, "Maggiorazione per urgenza");

        // Assert
        debitNote.Items.Should().HaveCount(1);
        debitNote.Items[0].Description.Should().Be("Maggiorazione urgenza");
        debitNote.Items[0].Imponibile.Should().Be(300m);
        debitNote.Items[0].IvaAmount.Should().Be(66m);
    }

    [Fact]
    public void CreateDebitNote_CalculatesTotalsFromAdditionalItems()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var additionalItems = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Servizio A",
                Quantity = 1,
                UnitPrice = 200m,
                IvaRate = IvaRate.Standard,
                Imponibile = 200m,
                IvaAmount = 44m,
                Total = 244m
            },
            new InvoiceItem
            {
                Description = "Servizio B",
                Quantity = 2,
                UnitPrice = 150m,
                IvaRate = IvaRate.Reduced,
                Imponibile = 300m,
                IvaAmount = 30m,
                Total = 330m
            }
        };

        // Act
        var debitNote = _sut.CreateDebitNote(original, additionalItems, "Integrazione");

        // Assert
        debitNote.ImponibileTotal.Should().Be(500m);   // 200 + 300
        debitNote.IvaTotal.Should().Be(74m);            // 44 + 30
        debitNote.SubTotal.Should().Be(574m);           // 500 + 74
        debitNote.TotalDue.Should().Be(574m);
    }

    [Fact]
    public void CreateDebitNote_SetsReasonInNotesAndCausale()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var reason = "Variazione in aumento ex Art. 26, comma 1, DPR 633/72";
        var additionalItems = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Supplemento",
                Quantity = 1,
                UnitPrice = 50m,
                IvaRate = IvaRate.Standard,
                Imponibile = 50m,
                IvaAmount = 11m,
                Total = 61m
            }
        };

        // Act
        var debitNote = _sut.CreateDebitNote(original, additionalItems, reason);

        // Assert
        debitNote.Notes.Should().Be(reason);
        debitNote.Causale.Should().Be(reason);
    }

    [Fact]
    public void CreateDebitNote_GeneratesNewId()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var additionalItems = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                Description = "Extra",
                Quantity = 1,
                UnitPrice = 100m,
                IvaRate = IvaRate.Standard,
                Imponibile = 100m,
                IvaAmount = 22m,
                Total = 122m
            }
        };

        // Act
        var debitNote = _sut.CreateDebitNote(original, additionalItems, "Integrazione");

        // Assert
        debitNote.Id.Should().NotBe(Guid.Empty);
        debitNote.Id.Should().NotBe(original.Id);
    }

    #endregion

    #region ValidateCreditNote Tests

    [Fact]
    public void ValidateCreditNote_ValidCreditNote_ReturnsIsValidTrue()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var creditNote = _sut.CreateCreditNote(original, "Annullamento");

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, original);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateCreditNote_WrongDocumentType_ReturnsError()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var invalidNote = new Invoice
        {
            DocumentType = DocumentType.TD01, // Wrong: should be TD04 or TD05
            RelatedInvoiceId = original.Id,
            RelatedInvoiceNumber = original.InvoiceNumber,
            ImponibileTotal = -1000m,
            IvaTotal = -220m
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(invalidNote, original);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Contains("TD04") && e.Contains("TD05"));
    }

    [Fact]
    public void ValidateCreditNote_MissingRelatedInvoiceId_ReturnsError()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var creditNote = new Invoice
        {
            DocumentType = DocumentType.TD04,
            RelatedInvoiceId = null, // Missing
            RelatedInvoiceNumber = "2026/001",
            ImponibileTotal = -1000m,
            IvaTotal = -220m
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, original);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("RelatedInvoiceId"));
    }

    [Fact]
    public void ValidateCreditNote_EmptyGuidRelatedInvoiceId_ReturnsError()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var creditNote = new Invoice
        {
            DocumentType = DocumentType.TD04,
            RelatedInvoiceId = Guid.Empty, // Empty GUID
            RelatedInvoiceNumber = "2026/001",
            ImponibileTotal = -1000m,
            IvaTotal = -220m
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, original);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("RelatedInvoiceId"));
    }

    [Fact]
    public void ValidateCreditNote_MissingRelatedInvoiceNumber_ReturnsError()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var creditNote = new Invoice
        {
            DocumentType = DocumentType.TD04,
            RelatedInvoiceId = original.Id,
            RelatedInvoiceNumber = null, // Missing
            ImponibileTotal = -1000m,
            IvaTotal = -220m
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, original);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("RelatedInvoiceNumber"));
    }

    [Fact]
    public void ValidateCreditNote_EmptyRelatedInvoiceNumber_ReturnsError()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        var creditNote = new Invoice
        {
            DocumentType = DocumentType.TD04,
            RelatedInvoiceId = original.Id,
            RelatedInvoiceNumber = "   ", // Whitespace only
            ImponibileTotal = -1000m,
            IvaTotal = -220m
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, original);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("RelatedInvoiceNumber"));
    }

    [Fact]
    public void ValidateCreditNote_OriginalInvoiceIsNull_ReturnsError()
    {
        // Arrange
        var creditNote = new Invoice
        {
            DocumentType = DocumentType.TD04,
            RelatedInvoiceId = Guid.NewGuid(),
            RelatedInvoiceNumber = "2026/001",
            ImponibileTotal = -500m,
            IvaTotal = -110m
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, null);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("non esiste"));
    }

    [Fact]
    public void ValidateCreditNote_OriginalInvoiceIsDraft_ReturnsError()
    {
        // Arrange
        var draftInvoice = CreateDraftInvoice();
        var creditNote = new Invoice
        {
            DocumentType = DocumentType.TD04,
            RelatedInvoiceId = draftInvoice.Id,
            RelatedInvoiceNumber = "DRAFT",
            ImponibileTotal = -500m,
            IvaTotal = -110m
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, draftInvoice);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Draft"));
    }

    [Fact]
    public void ValidateCreditNote_OriginalInvoiceIsCancelled_ReturnsError()
    {
        // Arrange
        var cancelledInvoice = CreateIssuedInvoice();
        cancelledInvoice.Status = InvoiceStatus.Cancelled;

        var creditNote = new Invoice
        {
            DocumentType = DocumentType.TD04,
            RelatedInvoiceId = cancelledInvoice.Id,
            RelatedInvoiceNumber = cancelledInvoice.InvoiceNumber,
            ImponibileTotal = -1000m,
            IvaTotal = -220m
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, cancelledInvoice);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Cancelled"));
    }

    [Theory]
    [InlineData(InvoiceStatus.Issued)]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Overdue)]
    public void ValidateCreditNote_OriginalInvoiceInValidStatus_ReturnsIsValidTrue(InvoiceStatus status)
    {
        // Arrange
        var original = CreateIssuedInvoice();
        original.Status = status;
        var creditNote = _sut.CreateCreditNote(original, "Nota di credito");

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, original);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateCreditNote_ImponibileExceedsOriginal_ReturnsError()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        // Original ImponibileTotal = 1000 EUR

        var creditNote = new Invoice
        {
            DocumentType = DocumentType.TD04,
            RelatedInvoiceId = original.Id,
            RelatedInvoiceNumber = original.InvoiceNumber,
            ImponibileTotal = -1500m, // Exceeds original 1000
            IvaTotal = -220m
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, original);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("imponibile") && e.Contains("supera"));
    }

    [Fact]
    public void ValidateCreditNote_IvaExceedsOriginal_ReturnsError()
    {
        // Arrange
        var original = CreateIssuedInvoice();
        // Original IvaTotal = 220 EUR

        var creditNote = new Invoice
        {
            DocumentType = DocumentType.TD04,
            RelatedInvoiceId = original.Id,
            RelatedInvoiceNumber = original.InvoiceNumber,
            ImponibileTotal = -1000m,
            IvaTotal = -300m // Exceeds original 220
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, original);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("IVA") && e.Contains("supera"));
    }

    [Fact]
    public void ValidateCreditNote_AmountsEqualToOriginal_ReturnsIsValidTrue()
    {
        // Arrange — Full credit note (same amounts as original, but negated)
        var original = CreateIssuedInvoice();
        var creditNote = _sut.CreateCreditNote(original, "Annullamento totale");

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, original);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateCreditNote_PartialCreditNote_AmountsLessThanOriginal_ReturnsIsValidTrue()
    {
        // Arrange — Partial credit (less than original amounts)
        var original = CreateIssuedInvoice();
        // Original: Imponibile=1000, IVA=220

        var partialCreditNote = new Invoice
        {
            DocumentType = DocumentType.TD04,
            RelatedInvoiceId = original.Id,
            RelatedInvoiceNumber = original.InvoiceNumber,
            ImponibileTotal = -500m, // Partial: only 500 of 1000
            IvaTotal = -110m         // Partial: only 110 of 220
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(partialCreditNote, original);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateCreditNote_DebitNoteTD05_DoesNotCheckAmountExceeding()
    {
        // Arrange — Debit notes (TD05) can have any amount (they are increases)
        var original = CreateIssuedInvoice();
        // Original: Imponibile=1000

        var debitNote = new Invoice
        {
            DocumentType = DocumentType.TD05,
            RelatedInvoiceId = original.Id,
            RelatedInvoiceNumber = original.InvoiceNumber,
            ImponibileTotal = 5000m, // Larger than original — valid for debit note
            IvaTotal = 1100m
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(debitNote, original);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateCreditNote_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange — Multiple validation failures at once
        var creditNote = new Invoice
        {
            DocumentType = DocumentType.TD01, // Wrong type
            RelatedInvoiceId = null,           // Missing
            RelatedInvoiceNumber = null        // Missing
        };

        // Act
        var (isValid, errors) = _sut.ValidateCreditNote(creditNote, null);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().HaveCountGreaterThanOrEqualTo(3);
        // Should have errors for: wrong type, missing RelatedInvoiceId, missing RelatedInvoiceNumber, missing original
    }

    #endregion
}
