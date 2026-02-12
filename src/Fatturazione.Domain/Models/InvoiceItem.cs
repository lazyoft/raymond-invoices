namespace Fatturazione.Domain.Models;

/// <summary>
/// Single line item on an invoice
/// </summary>
public class InvoiceItem
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Description of the service or product
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price (excluding VAT)
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// IVA rate applied to this item
    /// </summary>
    public IvaRate IvaRate { get; set; }

    /// <summary>
    /// Discount percentage (stub for future feature)
    /// </summary>
    public decimal DiscountPercentage { get; set; } = 0;

    /// <summary>
    /// Discount amount (stub for future feature)
    /// </summary>
    public decimal DiscountAmount { get; set; } = 0;

    // Calculated fields

    /// <summary>
    /// Subtotal before discount (Quantity × UnitPrice)
    /// </summary>
    public decimal Subtotal => Quantity * UnitPrice;

    /// <summary>
    /// Imponibile - Taxable amount after discount
    /// </summary>
    public decimal Imponibile { get; set; }

    /// <summary>
    /// IVA amount for this item
    /// </summary>
    public decimal IvaAmount { get; set; }

    /// <summary>
    /// Total for this item (Imponibile + IVA)
    /// </summary>
    public decimal Total { get; set; }
}
