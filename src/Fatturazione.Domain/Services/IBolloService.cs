using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Service for calculating "Imposta di Bollo" (stamp duty) on invoices
/// Per DPR 642/72 Art. 13: 2.00 EUR for non-VAT invoices over 77.47 EUR
/// </summary>
public interface IBolloService
{
    /// <summary>
    /// Determines if stamp duty (bollo) is required for the invoice
    /// </summary>
    /// <param name="invoice">Invoice to evaluate</param>
    /// <returns>True if bollo is required, false otherwise</returns>
    bool RequiresBollo(Invoice invoice);

    /// <summary>
    /// Calculates the stamp duty amount for the invoice
    /// </summary>
    /// <param name="invoice">Invoice to calculate bollo for</param>
    /// <returns>2.00 EUR if required, 0 otherwise</returns>
    decimal CalculateBollo(Invoice invoice);
}
