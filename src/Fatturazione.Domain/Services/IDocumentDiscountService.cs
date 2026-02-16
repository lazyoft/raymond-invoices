namespace Fatturazione.Domain.Services;

/// <summary>
/// Service for applying document-level discounts (Specifiche FatturaPA, blocco 2.1.1.8).
/// Document-level discounts reduce the total imponibile after summing all line items,
/// before computing IVA and ritenuta.
/// </summary>
public interface IDocumentDiscountService
{
    /// <summary>
    /// Applies document-level discounts to the imponibile total.
    /// First applies the percentage discount, then subtracts the fixed amount.
    /// Result is rounded to 2 decimal places and cannot go below zero.
    /// </summary>
    /// <param name="imponibileTotal">Sum of all line items' imponibile amounts</param>
    /// <param name="discountPercentage">Percentage discount (0-100)</param>
    /// <param name="discountAmount">Fixed discount amount in EUR</param>
    /// <returns>Discounted imponibile total, rounded to 2 decimal places, minimum 0</returns>
    decimal ApplyDocumentDiscount(decimal imponibileTotal, decimal discountPercentage, decimal discountAmount);
}
