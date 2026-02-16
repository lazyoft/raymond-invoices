using System.Xml.Linq;
using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Services;

/// <summary>
/// Tests for FatturaPAXmlService — FatturaPA XML Generation
/// DL 119/2018, DL 127/2015, Provvedimento AdE 89757/2018
/// </summary>
public class FatturaPAXmlServiceTests
{
    private readonly FatturaPAXmlService _sut;
    private static readonly XNamespace Ns = "http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2";

    public FatturaPAXmlServiceTests()
    {
        _sut = new FatturaPAXmlService();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a standard IssuerProfile (regime ordinario RF01).
    /// </summary>
    private static IssuerProfile CreateIssuerProfile()
    {
        return new IssuerProfile
        {
            Id = Guid.NewGuid(),
            RagioneSociale = "Studio Rossi SRL",
            PartitaIva = "01234567890",
            CodiceFiscale = "01234567890",
            RegimeFiscale = "RF01",
            Indirizzo = new Address
            {
                Street = "Via Roma 1",
                City = "Milano",
                Province = "MI",
                PostalCode = "20100"
            },
            Telefono = "0212345678",
            Email = "info@studiorossi.it"
        };
    }

    /// <summary>
    /// Creates a forfettario IssuerProfile (regime forfettario RF19).
    /// </summary>
    private static IssuerProfile CreateForfettarioIssuerProfile()
    {
        return new IssuerProfile
        {
            Id = Guid.NewGuid(),
            RagioneSociale = "Mario Bianchi",
            PartitaIva = "09876543210",
            CodiceFiscale = "BNCMRA85A01F205X",
            RegimeFiscale = "RF19",
            Indirizzo = new Address
            {
                Street = "Via Garibaldi 42",
                City = "Roma",
                Province = "RM",
                PostalCode = "00100"
            }
        };
    }

    /// <summary>
    /// Creates a standard TD01 invoice with IVA 22% for a company client.
    /// Imponibile: 1000 EUR, IVA 22%: 220 EUR, SubTotal: 1220 EUR, TotalDue: 1220 EUR
    /// </summary>
    private static Invoice CreateStandardInvoice()
    {
        var clientId = Guid.NewGuid();

        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "2026/001",
            InvoiceDate = new DateTime(2026, 1, 15),
            DueDate = new DateTime(2026, 2, 15),
            ClientId = clientId,
            Client = new Client
            {
                Id = clientId,
                RagioneSociale = "Acme SRL",
                PartitaIva = "11223344556",
                CodiceFiscale = "11223344556",
                ClientType = ClientType.Company,
                Address = new Address
                {
                    Street = "Via Verdi 10",
                    City = "Torino",
                    Province = "TO",
                    PostalCode = "10100"
                },
                CodiceDestinatario = "ABC1234"
            },
            DocumentType = DocumentType.TD01,
            Status = InvoiceStatus.Issued,
            EsigibilitaIva = EsigibilitaIva.Immediata,
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
            TotalDue = 1220m,
            PaymentInfo = new PaymentInfo
            {
                Condizioni = PaymentCondition.TP02_Completo,
                Modalita = PaymentMethod.MP05_Bonifico,
                IBAN = "IT60X0542811101000000123456"
            }
        };
    }

    /// <summary>
    /// Creates a forfettario invoice: no IVA, NaturaIva N2.2, bollo virtuale.
    /// Imponibile: 500 EUR, IVA: 0, BolloVirtuale: true, BolloAmount: 2.00
    /// </summary>
    private static Invoice CreateForfettarioInvoice()
    {
        var clientId = Guid.NewGuid();

        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "2026/010",
            InvoiceDate = new DateTime(2026, 2, 1),
            DueDate = new DateTime(2026, 3, 1),
            ClientId = clientId,
            Client = new Client
            {
                Id = clientId,
                RagioneSociale = "Beta SRL",
                PartitaIva = "99887766554",
                ClientType = ClientType.Company,
                Address = new Address
                {
                    Street = "Via Dante 5",
                    City = "Firenze",
                    Province = "FI",
                    PostalCode = "50100"
                },
                CodiceDestinatario = "XYZ7890"
            },
            DocumentType = DocumentType.TD01,
            Status = InvoiceStatus.Issued,
            IsRegimeForfettario = true,
            EsigibilitaIva = EsigibilitaIva.Immediata,
            BolloVirtuale = true,
            BolloAmount = 2.00m,
            Causale = "Operazione effettuata ai sensi dell'art. 1, commi 54-89, Legge n. 190/2014",
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Sviluppo applicazione web",
                    Quantity = 1,
                    UnitPrice = 500m,
                    IvaRate = IvaRate.Zero,
                    NaturaIva = NaturaIva.N2_2,
                    Imponibile = 500m,
                    IvaAmount = 0m,
                    Total = 500m
                }
            },
            ImponibileTotal = 500m,
            IvaTotal = 0m,
            SubTotal = 500m,
            RitenutaAmount = 0m,
            TotalDue = 502m
        };
    }

    /// <summary>
    /// Creates a PA invoice with split payment.
    /// Imponibile: 1000, IVA 22%: 220, EsigibilitaIva: SplitPayment, TotalDue: 1000 (IVA versata dalla PA)
    /// </summary>
    private static Invoice CreatePAInvoiceWithSplitPayment()
    {
        var clientId = Guid.NewGuid();

        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "2026/020",
            InvoiceDate = new DateTime(2026, 3, 1),
            DueDate = new DateTime(2026, 4, 1),
            ClientId = clientId,
            Client = new Client
            {
                Id = clientId,
                RagioneSociale = "Comune di Roma",
                PartitaIva = "02438750586",
                CodiceFiscale = "02438750586",
                ClientType = ClientType.PublicAdministration,
                SubjectToSplitPayment = true,
                Address = new Address
                {
                    Street = "Via del Campidoglio 1",
                    City = "Roma",
                    Province = "RM",
                    PostalCode = "00186"
                },
                CodiceUnivocoUfficio = "UFABCD",
                CIG = "ABC1234567",
                CUP = "CUP123456789AB"
            },
            DocumentType = DocumentType.TD01,
            Status = InvoiceStatus.Issued,
            EsigibilitaIva = EsigibilitaIva.SplitPayment,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Servizio di consulenza",
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
            RitenutaAmount = 0m,
            BolloAmount = 0m,
            TotalDue = 1000m,
            PaymentInfo = new PaymentInfo
            {
                Condizioni = PaymentCondition.TP02_Completo,
                Modalita = PaymentMethod.MP05_Bonifico,
                IBAN = "IT60X0542811101000000123456"
            }
        };
    }

    /// <summary>
    /// Creates a credit note (TD04) referencing an original invoice.
    /// </summary>
    private static Invoice CreateCreditNote()
    {
        var clientId = Guid.NewGuid();

        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "2026/030",
            InvoiceDate = new DateTime(2026, 3, 15),
            DueDate = new DateTime(2026, 4, 15),
            ClientId = clientId,
            Client = new Client
            {
                Id = clientId,
                RagioneSociale = "Gamma SPA",
                PartitaIva = "55443322110",
                ClientType = ClientType.Company,
                Address = new Address
                {
                    Street = "Corso Italia 20",
                    City = "Napoli",
                    Province = "NA",
                    PostalCode = "80100"
                },
                CodiceDestinatario = "DEF5678"
            },
            DocumentType = DocumentType.TD04,
            Status = InvoiceStatus.Issued,
            EsigibilitaIva = EsigibilitaIva.Immediata,
            RelatedInvoiceId = Guid.NewGuid(),
            RelatedInvoiceNumber = "2026/001",
            Causale = "Rettifica inesattezze fattura 2026/001",
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Consulenza informatica",
                    Quantity = 10,
                    UnitPrice = -100m,
                    IvaRate = IvaRate.Standard,
                    Imponibile = -1000m,
                    IvaAmount = -220m,
                    Total = -1220m
                }
            },
            ImponibileTotal = -1000m,
            IvaTotal = -220m,
            SubTotal = -1220m,
            RitenutaAmount = 0m,
            TotalDue = -1220m
        };
    }

    /// <summary>
    /// Creates an invoice with ritenuta d'acconto for a professional client.
    /// Imponibile: 1000, IVA 22%: 220, Ritenuta 20% of 1000: 200, TotalDue: 1020
    /// </summary>
    private static Invoice CreateInvoiceWithRitenuta()
    {
        var clientId = Guid.NewGuid();

        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "2026/040",
            InvoiceDate = new DateTime(2026, 4, 1),
            DueDate = new DateTime(2026, 5, 1),
            ClientId = clientId,
            Client = new Client
            {
                Id = clientId,
                RagioneSociale = "Dott. Mario Rossi",
                PartitaIva = "44332211009",
                CodiceFiscale = "RSSMRA80A01H501Z",
                ClientType = ClientType.Professional,
                SubjectToRitenuta = true,
                RitenutaPercentage = 20m,
                TipoRitenuta = TipoRitenuta.RT01,
                CausalePagamento = CausalePagamento.A,
                Address = new Address
                {
                    Street = "Via Manzoni 15",
                    City = "Bologna",
                    Province = "BO",
                    PostalCode = "40100"
                },
                CodiceDestinatario = "GHI9012"
            },
            DocumentType = DocumentType.TD01,
            Status = InvoiceStatus.Issued,
            EsigibilitaIva = EsigibilitaIva.Immediata,
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
            TotalDue = 1020m,
            PaymentInfo = new PaymentInfo
            {
                Condizioni = PaymentCondition.TP02_Completo,
                Modalita = PaymentMethod.MP05_Bonifico,
                IBAN = "IT60X0542811101000000654321"
            }
        };
    }

    /// <summary>
    /// Creates an invoice for a client using PEC (no CodiceDestinatario).
    /// </summary>
    private static Invoice CreateInvoiceWithPEC()
    {
        var clientId = Guid.NewGuid();

        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "2026/050",
            InvoiceDate = new DateTime(2026, 5, 1),
            DueDate = new DateTime(2026, 6, 1),
            ClientId = clientId,
            Client = new Client
            {
                Id = clientId,
                RagioneSociale = "Delta Consulting SRL",
                PartitaIva = "66778899001",
                ClientType = ClientType.Company,
                Address = new Address
                {
                    Street = "Piazza Duomo 3",
                    City = "Palermo",
                    Province = "PA",
                    PostalCode = "90100"
                },
                CodiceDestinatario = null,
                PEC = "delta@pec.it"
            },
            DocumentType = DocumentType.TD01,
            Status = InvoiceStatus.Issued,
            EsigibilitaIva = EsigibilitaIva.Immediata,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Servizio base",
                    Quantity = 1,
                    UnitPrice = 200m,
                    IvaRate = IvaRate.Standard,
                    Imponibile = 200m,
                    IvaAmount = 44m,
                    Total = 244m
                }
            },
            ImponibileTotal = 200m,
            IvaTotal = 44m,
            SubTotal = 244m,
            TotalDue = 244m
        };
    }

    /// <summary>
    /// Parses the generated XML and returns the root element.
    /// </summary>
    private static XElement ParseXml(string xml)
    {
        var doc = XDocument.Parse(xml);
        return doc.Root!;
    }

    /// <summary>
    /// Gets the Header element from parsed XML root.
    /// </summary>
    private static XElement GetHeader(XElement root)
    {
        return root.Element(Ns + "FatturaElettronicaHeader")
            ?? root.Element("FatturaElettronicaHeader")!;
    }

    /// <summary>
    /// Gets the Body element from parsed XML root.
    /// </summary>
    private static XElement GetBody(XElement root)
    {
        return root.Element(Ns + "FatturaElettronicaBody")
            ?? root.Element("FatturaElettronicaBody")!;
    }

    #endregion

    #region XML Well-Formedness Tests

    [Fact]
    public void GenerateXml_StandardInvoice_ProducesWellFormedXml()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);

        // Assert — XDocument.Parse will throw if XML is not well-formed
        var doc = XDocument.Parse(xml);
        doc.Should().NotBeNull();
        doc.Root.Should().NotBeNull();
    }

    [Fact]
    public void GenerateXml_StandardInvoice_HasCorrectRootElement()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);

        // Assert
        root.Name.Should().Be(Ns + "FatturaElettronica");
        root.Attribute("versione")!.Value.Should().Be("FPR12");
    }

    #endregion

    #region Standard Invoice (TD01) Tests

    [Fact]
    public void GenerateXml_StandardInvoice_HasCorrectDatiTrasmissione()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var header = GetHeader(root);
        var datiTrasmissione = header.Element("DatiTrasmissione")!;

        // Assert
        var idTrasmittente = datiTrasmissione.Element("IdTrasmittente")!;
        idTrasmittente.Element("IdPaese")!.Value.Should().Be("IT");
        idTrasmittente.Element("IdCodice")!.Value.Should().Be("01234567890");

        // ProgressivoInvio: "2026/001" without "/" → "2026001"
        datiTrasmissione.Element("ProgressivoInvio")!.Value.Should().Be("2026001");
        datiTrasmissione.Element("FormatoTrasmissione")!.Value.Should().Be("FPR12");
        datiTrasmissione.Element("CodiceDestinatario")!.Value.Should().Be("ABC1234");
    }

    [Fact]
    public void GenerateXml_StandardInvoice_HasCorrectCedentePrestatore()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var header = GetHeader(root);
        var cedente = header.Element("CedentePrestatore")!;
        var datiAnagrafici = cedente.Element("DatiAnagrafici")!;

        // Assert
        var idFiscale = datiAnagrafici.Element("IdFiscaleIVA")!;
        idFiscale.Element("IdPaese")!.Value.Should().Be("IT");
        idFiscale.Element("IdCodice")!.Value.Should().Be("01234567890");

        datiAnagrafici.Element("Anagrafica")!.Element("Denominazione")!.Value
            .Should().Be("Studio Rossi SRL");
        datiAnagrafici.Element("RegimeFiscale")!.Value.Should().Be("RF01");

        var sede = cedente.Element("Sede")!;
        sede.Element("Indirizzo")!.Value.Should().Be("Via Roma 1");
        sede.Element("CAP")!.Value.Should().Be("20100");
        sede.Element("Comune")!.Value.Should().Be("Milano");
        sede.Element("Provincia")!.Value.Should().Be("MI");
        sede.Element("Nazione")!.Value.Should().Be("IT");
    }

    [Fact]
    public void GenerateXml_StandardInvoice_HasCorrectCessionarioCommittente()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var header = GetHeader(root);
        var cessionario = header.Element("CessionarioCommittente")!;
        var datiAnagrafici = cessionario.Element("DatiAnagrafici")!;

        // Assert
        var idFiscale = datiAnagrafici.Element("IdFiscaleIVA")!;
        idFiscale.Element("IdPaese")!.Value.Should().Be("IT");
        idFiscale.Element("IdCodice")!.Value.Should().Be("11223344556");

        datiAnagrafici.Element("CodiceFiscale")!.Value.Should().Be("11223344556");
        datiAnagrafici.Element("Anagrafica")!.Element("Denominazione")!.Value
            .Should().Be("Acme SRL");

        var sede = cessionario.Element("Sede")!;
        sede.Element("Indirizzo")!.Value.Should().Be("Via Verdi 10");
        sede.Element("CAP")!.Value.Should().Be("10100");
        sede.Element("Comune")!.Value.Should().Be("Torino");
        sede.Element("Provincia")!.Value.Should().Be("TO");
        sede.Element("Nazione")!.Value.Should().Be("IT");
    }

    [Fact]
    public void GenerateXml_StandardInvoice_HasCorrectDatiGeneraliDocumento()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiGenerali = body.Element("DatiGenerali")!;
        var datiDoc = datiGenerali.Element("DatiGeneraliDocumento")!;

        // Assert
        datiDoc.Element("TipoDocumento")!.Value.Should().Be("TD01");
        datiDoc.Element("Divisa")!.Value.Should().Be("EUR");
        datiDoc.Element("Data")!.Value.Should().Be("2026-01-15");
        datiDoc.Element("Numero")!.Value.Should().Be("2026/001");
    }

    [Fact]
    public void GenerateXml_StandardInvoice_HasCorrectDettaglioLinee()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiBeniServizi = body.Element("DatiBeniServizi")!;
        var dettaglio = datiBeniServizi.Elements("DettaglioLinee").First();

        // Assert
        dettaglio.Element("NumeroLinea")!.Value.Should().Be("1");
        dettaglio.Element("Descrizione")!.Value.Should().Be("Consulenza informatica");
        dettaglio.Element("Quantita")!.Value.Should().Be("10.00");
        dettaglio.Element("PrezzoUnitario")!.Value.Should().Be("100.00");
        dettaglio.Element("PrezzoTotale")!.Value.Should().Be("1000.00");
        dettaglio.Element("AliquotaIVA")!.Value.Should().Be("22.00");
    }

    [Fact]
    public void GenerateXml_StandardInvoice_HasCorrectDatiRiepilogo()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiBeniServizi = body.Element("DatiBeniServizi")!;
        var riepilogo = datiBeniServizi.Elements("DatiRiepilogo").First();

        // Assert
        riepilogo.Element("AliquotaIVA")!.Value.Should().Be("22.00");
        riepilogo.Element("ImponibileImporto")!.Value.Should().Be("1000.00");
        riepilogo.Element("Imposta")!.Value.Should().Be("220.00");
        riepilogo.Element("EsigibilitaIVA")!.Value.Should().Be("I");
    }

    [Fact]
    public void GenerateXml_StandardInvoice_HasCorrectDatiPagamento()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiPagamento = body.Element("DatiPagamento")!;

        // Assert
        datiPagamento.Element("CondizioniPagamento")!.Value.Should().Be("TP02");

        var dettaglio = datiPagamento.Element("DettaglioPagamento")!;
        dettaglio.Element("ModalitaPagamento")!.Value.Should().Be("MP05");
        dettaglio.Element("DataScadenzaPagamento")!.Value.Should().Be("2026-02-15");
        dettaglio.Element("ImportoPagamento")!.Value.Should().Be("1220.00");
        dettaglio.Element("IBAN")!.Value.Should().Be("IT60X0542811101000000123456");
    }

    [Fact]
    public void GenerateXml_StandardInvoice_NoBolloVirtualeSection()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiDoc = body.Element("DatiGenerali")!.Element("DatiGeneraliDocumento")!;

        // Assert — no DatiBollo when BolloVirtuale is false
        datiDoc.Element("DatiBollo").Should().BeNull();
    }

    [Fact]
    public void GenerateXml_StandardInvoice_NoDatiRitenutaSection()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiDoc = body.Element("DatiGenerali")!.Element("DatiGeneraliDocumento")!;

        // Assert — no DatiRitenuta for company client
        datiDoc.Element("DatiRitenuta").Should().BeNull();
    }

    [Fact]
    public void GenerateXml_StandardInvoice_NoDatiFattureCollegate()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiGenerali = body.Element("DatiGenerali")!;

        // Assert — no DatiFattureCollegate for a regular invoice
        datiGenerali.Element("DatiFattureCollegate").Should().BeNull();
    }

    #endregion

    #region Forfettario Invoice Tests

    [Fact]
    public void GenerateXml_ForfettarioInvoice_HasNaturaIvaN2_2()
    {
        // Arrange — Forfettario: no IVA, NaturaIva N2.2 (Legge 190/2014)
        var invoice = CreateForfettarioInvoice();
        var issuer = CreateForfettarioIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiBeniServizi = body.Element("DatiBeniServizi")!;
        var dettaglio = datiBeniServizi.Elements("DettaglioLinee").First();

        // Assert
        dettaglio.Element("AliquotaIVA")!.Value.Should().Be("0.00");
        dettaglio.Element("Natura")!.Value.Should().Be("N2.2");
    }

    [Fact]
    public void GenerateXml_ForfettarioInvoice_HasZeroIvaInRiepilogo()
    {
        // Arrange
        var invoice = CreateForfettarioInvoice();
        var issuer = CreateForfettarioIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiBeniServizi = body.Element("DatiBeniServizi")!;
        var riepilogo = datiBeniServizi.Elements("DatiRiepilogo").First();

        // Assert
        riepilogo.Element("AliquotaIVA")!.Value.Should().Be("0.00");
        riepilogo.Element("Imposta")!.Value.Should().Be("0.00");
        riepilogo.Element("Natura")!.Value.Should().Be("N2.2");
    }

    [Fact]
    public void GenerateXml_ForfettarioInvoice_HasRegimeFiscaleRF19()
    {
        // Arrange
        var invoice = CreateForfettarioInvoice();
        var issuer = CreateForfettarioIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var header = GetHeader(root);
        var cedente = header.Element("CedentePrestatore")!;
        var datiAnagrafici = cedente.Element("DatiAnagrafici")!;

        // Assert
        datiAnagrafici.Element("RegimeFiscale")!.Value.Should().Be("RF19");
    }

    [Fact]
    public void GenerateXml_ForfettarioInvoice_HasCausale()
    {
        // Arrange
        var invoice = CreateForfettarioInvoice();
        var issuer = CreateForfettarioIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiDoc = body.Element("DatiGenerali")!.Element("DatiGeneraliDocumento")!;

        // Assert
        datiDoc.Element("Causale")!.Value.Should().Contain("Legge n. 190/2014");
    }

    #endregion

    #region PA Invoice with Split Payment Tests

    [Fact]
    public void GenerateXml_PAInvoice_HasFormatoTrasmissioneFPA12()
    {
        // Arrange
        var invoice = CreatePAInvoiceWithSplitPayment();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);

        // Assert — PA invoices use FPA12 format
        root.Attribute("versione")!.Value.Should().Be("FPA12");
    }

    [Fact]
    public void GenerateXml_PAInvoice_HasCodiceUnivocoUfficio()
    {
        // Arrange
        var invoice = CreatePAInvoiceWithSplitPayment();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var header = GetHeader(root);
        var datiTrasmissione = header.Element("DatiTrasmissione")!;

        // Assert — PA uses CodiceUnivocoUfficio as CodiceDestinatario
        datiTrasmissione.Element("CodiceDestinatario")!.Value.Should().Be("UFABCD");
    }

    [Fact]
    public void GenerateXml_PAInvoice_HasEsigibilitaIvaSplitPayment()
    {
        // Arrange — Art. 17-ter DPR 633/72
        var invoice = CreatePAInvoiceWithSplitPayment();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiBeniServizi = body.Element("DatiBeniServizi")!;
        var riepilogo = datiBeniServizi.Elements("DatiRiepilogo").First();

        // Assert
        riepilogo.Element("EsigibilitaIVA")!.Value.Should().Be("S");
    }

    [Fact]
    public void GenerateXml_PAInvoice_NoPECDestinatarioElement()
    {
        // Arrange — PA with CodiceUnivocoUfficio should not have PECDestinatario
        var invoice = CreatePAInvoiceWithSplitPayment();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var header = GetHeader(root);
        var datiTrasmissione = header.Element("DatiTrasmissione")!;

        // Assert
        datiTrasmissione.Element("PECDestinatario").Should().BeNull();
    }

    #endregion

    #region Credit Note (TD04) Tests

    [Fact]
    public void GenerateXml_CreditNote_HasDocumentTypeTD04()
    {
        // Arrange — Art. 26 DPR 633/72
        var invoice = CreateCreditNote();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiDoc = body.Element("DatiGenerali")!.Element("DatiGeneraliDocumento")!;

        // Assert
        datiDoc.Element("TipoDocumento")!.Value.Should().Be("TD04");
    }

    [Fact]
    public void GenerateXml_CreditNote_HasDatiFattureCollegate()
    {
        // Arrange
        var invoice = CreateCreditNote();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiGenerali = body.Element("DatiGenerali")!;
        var datiFattureCollegate = datiGenerali.Element("DatiFattureCollegate")!;

        // Assert — Related invoice reference
        datiFattureCollegate.Element("IdDocumento")!.Value.Should().Be("2026/001");
    }

    [Fact]
    public void GenerateXml_CreditNote_HasCausale()
    {
        // Arrange
        var invoice = CreateCreditNote();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiDoc = body.Element("DatiGenerali")!.Element("DatiGeneraliDocumento")!;

        // Assert
        datiDoc.Element("Causale")!.Value.Should().Contain("Rettifica");
    }

    #endregion

    #region NaturaIva Format Tests

    [Theory]
    [InlineData(NaturaIva.N1, "N1")]
    [InlineData(NaturaIva.N2_1, "N2.1")]
    [InlineData(NaturaIva.N2_2, "N2.2")]
    [InlineData(NaturaIva.N3_1, "N3.1")]
    [InlineData(NaturaIva.N3_2, "N3.2")]
    [InlineData(NaturaIva.N3_3, "N3.3")]
    [InlineData(NaturaIva.N3_4, "N3.4")]
    [InlineData(NaturaIva.N3_5, "N3.5")]
    [InlineData(NaturaIva.N3_6, "N3.6")]
    [InlineData(NaturaIva.N4, "N4")]
    [InlineData(NaturaIva.N5, "N5")]
    [InlineData(NaturaIva.N6_1, "N6.1")]
    [InlineData(NaturaIva.N6_2, "N6.2")]
    [InlineData(NaturaIva.N6_3, "N6.3")]
    [InlineData(NaturaIva.N6_4, "N6.4")]
    [InlineData(NaturaIva.N6_5, "N6.5")]
    [InlineData(NaturaIva.N6_6, "N6.6")]
    [InlineData(NaturaIva.N6_7, "N6.7")]
    [InlineData(NaturaIva.N6_8, "N6.8")]
    [InlineData(NaturaIva.N6_9, "N6.9")]
    [InlineData(NaturaIva.N7, "N7")]
    public void FormatNaturaIva_ConvertsEnumToCorrectCode(NaturaIva natura, string expectedCode)
    {
        // Act
        var result = FatturaPAXmlService.FormatNaturaIva(natura);

        // Assert — Underscore must become dot: N2_1 → "N2.1", not "N2_1"
        result.Should().Be(expectedCode);
        result.Should().NotContain("_");
    }

    #endregion

    #region EsigibilitaIva Format Tests

    [Theory]
    [InlineData(EsigibilitaIva.Immediata, "I")]
    [InlineData(EsigibilitaIva.Differita, "D")]
    [InlineData(EsigibilitaIva.SplitPayment, "S")]
    public void FormatEsigibilitaIva_ConvertsEnumToCorrectCode(EsigibilitaIva esigibilita, string expectedCode)
    {
        // Act
        var result = FatturaPAXmlService.FormatEsigibilitaIva(esigibilita);

        // Assert
        result.Should().Be(expectedCode);
    }

    #endregion

    #region PaymentMethod Format Tests

    [Theory]
    [InlineData(PaymentMethod.MP01_Contanti, "MP01")]
    [InlineData(PaymentMethod.MP05_Bonifico, "MP05")]
    [InlineData(PaymentMethod.MP08_CartaDiPagamento, "MP08")]
    [InlineData(PaymentMethod.MP12_RIBA, "MP12")]
    [InlineData(PaymentMethod.MP23_PagoPA, "MP23")]
    public void FormatPaymentMethod_ExtractsCodeBeforeUnderscore(PaymentMethod method, string expectedCode)
    {
        // Act
        var result = FatturaPAXmlService.FormatPaymentMethod(method);

        // Assert
        result.Should().Be(expectedCode);
    }

    #endregion

    #region PaymentCondition Format Tests

    [Theory]
    [InlineData(PaymentCondition.TP01_Rate, "TP01")]
    [InlineData(PaymentCondition.TP02_Completo, "TP02")]
    [InlineData(PaymentCondition.TP03_Anticipo, "TP03")]
    public void FormatPaymentCondition_ExtractsCodeBeforeUnderscore(PaymentCondition condition, string expectedCode)
    {
        // Act
        var result = FatturaPAXmlService.FormatPaymentCondition(condition);

        // Assert
        result.Should().Be(expectedCode);
    }

    #endregion

    #region BolloVirtuale Tests

    [Fact]
    public void GenerateXml_WithBolloVirtuale_HasDatiBolloSection()
    {
        // Arrange — Art. 13 DPR 642/72
        var invoice = CreateForfettarioInvoice();
        var issuer = CreateForfettarioIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiDoc = body.Element("DatiGenerali")!.Element("DatiGeneraliDocumento")!;
        var datiBollo = datiDoc.Element("DatiBollo")!;

        // Assert
        datiBollo.Element("BolloVirtuale")!.Value.Should().Be("SI");
        datiBollo.Element("ImportoBollo")!.Value.Should().Be("2.00");
    }

    #endregion

    #region DatiRitenuta Tests

    [Fact]
    public void GenerateXml_WithRitenuta_HasDatiRitenutaSection()
    {
        // Arrange — Art. 25 DPR 600/73
        var invoice = CreateInvoiceWithRitenuta();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiDoc = body.Element("DatiGenerali")!.Element("DatiGeneraliDocumento")!;
        var datiRitenuta = datiDoc.Element("DatiRitenuta")!;

        // Assert
        datiRitenuta.Element("TipoRitenuta")!.Value.Should().Be("RT01");
        datiRitenuta.Element("ImportoRitenuta")!.Value.Should().Be("200.00");
        datiRitenuta.Element("AliquotaRitenuta")!.Value.Should().Be("20.00");
        datiRitenuta.Element("CausalePagamento")!.Value.Should().Be("A");
    }

    [Fact]
    public void GenerateXml_WithRitenuta_HasCorrectTotalDue()
    {
        // Arrange — TotalDue = SubTotal - Ritenuta = 1220 - 200 = 1020
        var invoice = CreateInvoiceWithRitenuta();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiPagamento = body.Element("DatiPagamento")!;
        var dettaglioPagamento = datiPagamento.Element("DettaglioPagamento")!;

        // Assert
        dettaglioPagamento.Element("ImportoPagamento")!.Value.Should().Be("1020.00");
    }

    #endregion

    #region PaymentInfo Tests

    [Fact]
    public void GenerateXml_WithPaymentInfo_HasDatiPagamentoSection()
    {
        // Arrange — Blocco 2.4 tracciato FatturaPA
        var invoice = CreateStandardInvoice();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);

        // Assert
        body.Element("DatiPagamento").Should().NotBeNull();
    }

    [Fact]
    public void GenerateXml_WithoutPaymentInfo_NoDatiPagamentoSection()
    {
        // Arrange
        var invoice = CreateCreditNote(); // Credit note has no PaymentInfo
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);

        // Assert
        body.Element("DatiPagamento").Should().BeNull();
    }

    [Fact]
    public void GenerateXml_WithPaymentInfoNoIBAN_NoIBANElement()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        invoice.PaymentInfo!.IBAN = null;
        invoice.PaymentInfo.Modalita = PaymentMethod.MP01_Contanti;
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var dettaglio = body.Element("DatiPagamento")!.Element("DettaglioPagamento")!;

        // Assert — No IBAN for cash payments
        dettaglio.Element("IBAN").Should().BeNull();
        dettaglio.Element("ModalitaPagamento")!.Value.Should().Be("MP01");
    }

    #endregion

    #region PEC Destinatario Tests

    [Fact]
    public void GenerateXml_ClientWithPEC_NoCodiceDestinatario_HasPECDestinatario()
    {
        // Arrange — When CodiceDestinatario is "0000000", PEC should be included
        var invoice = CreateInvoiceWithPEC();
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var header = GetHeader(root);
        var datiTrasmissione = header.Element("DatiTrasmissione")!;

        // Assert
        datiTrasmissione.Element("CodiceDestinatario")!.Value.Should().Be("0000000");
        datiTrasmissione.Element("PECDestinatario")!.Value.Should().Be("delta@pec.it");
    }

    [Fact]
    public void GenerateXml_ClientWithCodiceDestinatario_NoPECDestinatarioElement()
    {
        // Arrange — When CodiceDestinatario is set, no PEC element
        var invoice = CreateStandardInvoice();
        invoice.Client!.PEC = "acme@pec.it"; // PEC set but CodiceDestinatario is also set
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var header = GetHeader(root);
        var datiTrasmissione = header.Element("DatiTrasmissione")!;

        // Assert — CodiceDestinatario is set, so no PEC
        datiTrasmissione.Element("CodiceDestinatario")!.Value.Should().Be("ABC1234");
        datiTrasmissione.Element("PECDestinatario").Should().BeNull();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void GenerateXml_NullInvoice_ThrowsArgumentNullException()
    {
        // Arrange
        var issuer = CreateIssuerProfile();

        // Act & Assert
        var act = () => _sut.GenerateXml(null!, issuer);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateXml_NullIssuer_ThrowsArgumentNullException()
    {
        // Arrange
        var invoice = CreateStandardInvoice();

        // Act & Assert
        var act = () => _sut.GenerateXml(invoice, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateXml_NullClient_ThrowsArgumentException()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        invoice.Client = null;
        var issuer = CreateIssuerProfile();

        // Act & Assert
        var act = () => _sut.GenerateXml(invoice, issuer);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Client*");
    }

    #endregion

    #region Multiple IVA Rates Tests

    [Fact]
    public void GenerateXml_MultipleIvaRates_HasMultipleDettaglioLinee()
    {
        // Arrange — Invoice with 2 items at different IVA rates
        var invoice = CreateStandardInvoice();
        invoice.Items.Add(new InvoiceItem
        {
            Id = Guid.NewGuid(),
            Description = "Manutenzione",
            Quantity = 2,
            UnitPrice = 150m,
            IvaRate = IvaRate.Reduced,
            Imponibile = 300m,
            IvaAmount = 30m,
            Total = 330m
        });
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiBeniServizi = body.Element("DatiBeniServizi")!;

        // Assert
        var dettagli = datiBeniServizi.Elements("DettaglioLinee").ToList();
        dettagli.Should().HaveCount(2);
        dettagli[0].Element("NumeroLinea")!.Value.Should().Be("1");
        dettagli[1].Element("NumeroLinea")!.Value.Should().Be("2");

        var riepiloghi = datiBeniServizi.Elements("DatiRiepilogo").ToList();
        riepiloghi.Should().HaveCount(2);
    }

    [Fact]
    public void GenerateXml_MultipleIvaRates_HasCorrectRiepilogoPerRate()
    {
        // Arrange
        var invoice = CreateStandardInvoice();
        invoice.Items.Add(new InvoiceItem
        {
            Id = Guid.NewGuid(),
            Description = "Manutenzione",
            Quantity = 2,
            UnitPrice = 150m,
            IvaRate = IvaRate.Reduced,
            Imponibile = 300m,
            IvaAmount = 30m,
            Total = 330m
        });
        var issuer = CreateIssuerProfile();

        // Act
        var xml = _sut.GenerateXml(invoice, issuer);
        var root = ParseXml(xml);
        var body = GetBody(root);
        var datiBeniServizi = body.Element("DatiBeniServizi")!;
        var riepiloghi = datiBeniServizi.Elements("DatiRiepilogo").ToList();

        // Assert — Two riepilogo groups: 22% and 10%
        var standard = riepiloghi.First(r => r.Element("AliquotaIVA")!.Value == "22.00");
        standard.Element("ImponibileImporto")!.Value.Should().Be("1000.00");
        standard.Element("Imposta")!.Value.Should().Be("220.00");

        var reduced = riepiloghi.First(r => r.Element("AliquotaIVA")!.Value == "10.00");
        reduced.Element("ImponibileImporto")!.Value.Should().Be("300.00");
        reduced.Element("Imposta")!.Value.Should().Be("30.00");
    }

    #endregion
}
