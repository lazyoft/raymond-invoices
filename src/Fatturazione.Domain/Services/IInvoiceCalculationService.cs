using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Service for calculating invoice totals and amounts
/// </summary>
public interface IInvoiceCalculationService
{
    /// <summary>
    /// Calculates all totals for an invoice
    /// </summary>
    void CalculateInvoiceTotals(Invoice invoice);

    /// <summary>
    /// Calculates totals for a single invoice item
    /// </summary>
    void CalculateItemTotals(InvoiceItem item);

    /// <summary>
    /// Calculates ritenuta amount for an invoice
    /// </summary>
    decimal CalculateRitenutaAmount(Invoice invoice);

    /// <summary>
    /// Calculates IVA breakdown by rate
    /// </summary>
    Dictionary<IvaRate, decimal> CalculateIvaByRate(Invoice invoice);
}
