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
