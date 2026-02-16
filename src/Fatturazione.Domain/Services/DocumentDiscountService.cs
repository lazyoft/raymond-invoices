namespace Fatturazione.Domain.Services;

/// <summary>
/// Implementation of document-level discount calculation per Specifiche FatturaPA, blocco 2.1.1.8.
/// Sconti/maggiorazioni a livello documento riducono l'imponibile complessivo prima del calcolo IVA.
/// </summary>
public class DocumentDiscountService : IDocumentDiscountService
{
    /// <summary>
    /// Applies document-level discounts to the imponibile total.
    /// Order of operations:
    ///   1. Apply percentage discount: result = imponibile * (1 - percentage / 100)
    ///   2. Subtract fixed amount: result = result - discountAmount
    ///   3. Floor at zero (result cannot be negative)
    ///   4. Round to 2 decimal places
    /// </summary>
    public decimal ApplyDocumentDiscount(decimal imponibileTotal, decimal discountPercentage, decimal discountAmount)
    {
        // Step 1: Apply percentage discount
        var result = imponibileTotal * (1m - discountPercentage / 100m);

        // Step 2: Subtract fixed discount amount
        result -= discountAmount;

        // Step 3: Floor at zero
        if (result < 0m)
        {
            result = 0m;
        }

        // Step 4: Round to 2 decimal places
        return Math.Round(result, 2);
    }
}
