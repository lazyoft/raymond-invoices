namespace Fatturazione.Domain.Models;

/// <summary>
/// Italian invoice (Fattura)
/// </summary>
public class Invoice
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Invoice number in format YYYY/NNN (e.g., "2026/001")
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Invoice date (Data Fattura)
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// Payment due date (Scadenza)
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Client ID reference
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Client entity (navigation property)
    /// </summary>
    public Client? Client { get; set; }

    /// <summary>
    /// Tipo documento (TD01, TD04, TD05, ecc.) — Specifiche FatturaPA, campo 2.1.1.1
    /// </summary>
    public DocumentType DocumentType { get; set; } = DocumentType.TD01;

    /// <summary>
    /// Invoice status
    /// </summary>
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>
    /// Collection of invoice items
    /// </summary>
    public List<InvoiceItem> Items { get; set; } = new();

    /// <summary>
    /// Optional notes on the invoice
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Causale documento — Obbligatoria per regime forfettario
    /// Deve contenere riferimento normativo (Art. 1, commi 54-89, L. 190/2014)
    /// </summary>
    public string? Causale { get; set; }

    /// <summary>
    /// Data dell'operazione (se diversa dalla data fattura)
    /// Per fatture immediate: emissione entro 12 gg dall'operazione
    /// Per fatture differite (TD24): entro il 15 del mese successivo
    /// </summary>
    public DateTime? DataOperazione { get; set; }

    /// <summary>
    /// Indica se è una fattura semplificata (Art. 21-bis DPR 633/72)
    /// Importo massimo 400 EUR (senza limite per forfettari dal 01/01/2025)
    /// </summary>
    public bool IsSimplified { get; set; }

    /// <summary>
    /// Esigibilità IVA (I/D/S) — Specifiche FatturaPA, campo 2.2.2.7
    /// Derivata automaticamente: SplitPayment → S, altrimenti I (default)
    /// </summary>
    public EsigibilitaIva EsigibilitaIva { get; set; } = EsigibilitaIva.Immediata;

    /// <summary>
    /// Riferimento al profilo dell'emittente (cedente/prestatore)
    /// </summary>
    public Guid? IssuerProfileId { get; set; }

    /// <summary>
    /// Profilo emittente (navigation property)
    /// </summary>
    public IssuerProfile? IssuerProfile { get; set; }

    /// <summary>
    /// ID della fattura originaria (per note di credito/debito)
    /// </summary>
    public Guid? RelatedInvoiceId { get; set; }

    /// <summary>
    /// Numero della fattura originaria (per note di credito/debito)
    /// </summary>
    public string? RelatedInvoiceNumber { get; set; }

    /// <summary>
    /// Informazioni di pagamento (Specifiche FatturaPA, blocco 2.4)
    /// </summary>
    public PaymentInfo? PaymentInfo { get; set; }

    /// <summary>
    /// Sconto percentuale a livello documento
    /// </summary>
    public decimal DocumentDiscountPercentage { get; set; }

    /// <summary>
    /// Sconto fisso a livello documento
    /// </summary>
    public decimal DocumentDiscountAmount { get; set; }

    /// <summary>
    /// Bollo virtuale (SI/NO) per tracciato XML FatturaPA
    /// </summary>
    public bool BolloVirtuale { get; set; }

    // Calculated totals

    /// <summary>
    /// Total Imponibile (sum of all items' taxable amounts)
    /// </summary>
    public decimal ImponibileTotal { get; set; }

    /// <summary>
    /// Total IVA (sum of all items' VAT)
    /// </summary>
    public decimal IvaTotal { get; set; }

    /// <summary>
    /// Subtotal (Imponibile + IVA) before ritenuta
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Ritenuta d'Acconto amount (withholding tax)
    /// </summary>
    public decimal RitenutaAmount { get; set; }

    /// <summary>
    /// Indicates if the invoice is issued under "Regime Forfettario" (flat-rate tax regime)
    /// </summary>
    public bool IsRegimeForfettario { get; set; }

    /// <summary>
    /// Stamp duty amount (Imposta di Bollo)
    /// 2.00 EUR when required per DPR 642/72 Art. 13
    /// </summary>
    public decimal BolloAmount { get; set; }

    /// <summary>
    /// Total amount due (SubTotal - Ritenuta)
    /// Totale Documento
    /// </summary>
    public decimal TotalDue { get; set; }

    /// <summary>
    /// IVA breakdown by rate (for invoice detail)
    /// </summary>
    public Dictionary<IvaRate, decimal> IvaByRate { get; set; } = new();

    /// <summary>
    /// Date the invoice was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date the invoice was last modified
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}
