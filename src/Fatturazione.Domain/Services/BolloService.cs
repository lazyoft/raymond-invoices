using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Implementation of stamp duty calculation per DPR 642/72 Art. 13
/// </summary>
public class BolloService : IBolloService
{
    /// <summary>
    /// Threshold above which stamp duty is required (77.47 EUR)
    /// Per DPR 642/72 Art. 13
    /// </summary>
    private const decimal BolloThreshold = 77.47m;

    /// <summary>
    /// Fixed stamp duty amount (2.00 EUR)
    /// </summary>
    private const decimal BolloFixedAmount = 2.00m;

    /// <summary>
    /// Determines if stamp duty (bollo) is required for the invoice
    /// Required when: IsRegimeForfettario == true AND ImponibileTotal > 77.47
    /// </summary>
    public bool RequiresBollo(Invoice invoice)
    {
        if (!invoice.IsRegimeForfettario)
        {
            return false;
        }

        // Per DPR 642/72 Art. 13: threshold must be exceeded (not equal)
        return invoice.ImponibileTotal > BolloThreshold;
    }

    /// <summary>
    /// Calculates the stamp duty amount
    /// Returns 2.00 EUR if required, 0 otherwise
    /// </summary>
    public decimal CalculateBollo(Invoice invoice)
    {
        return RequiresBollo(invoice) ? BolloFixedAmount : 0m;
    }
}
