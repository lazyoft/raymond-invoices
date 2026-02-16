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
    /// NaturaIva codes that represent non-IVA operations subject to bollo.
    /// Per DPR 642/72 Art. 13:
    /// - N1: Escluse ex art. 15
    /// - N2_1, N2_2: Non soggette (fuori campo IVA)
    /// - N3_5, N3_6: Non imponibili senza diritto alla detrazione
    /// - N4: Esenti (art. 10 DPR 633/72)
    /// </summary>
    private static readonly HashSet<NaturaIva> BolloNaturaIvaCodes = new()
    {
        NaturaIva.N1,
        NaturaIva.N2_1,
        NaturaIva.N2_2,
        NaturaIva.N3_5,
        NaturaIva.N3_6,
        NaturaIva.N4
    };

    /// <summary>
    /// Determines if stamp duty (bollo) is required for the invoice.
    /// Required when the non-IVA portion exceeds 77.47 EUR. This includes:
    /// - Regime forfettario invoices (entire imponibile is non-IVA)
    /// - Items with NaturaIva in {N1, N2_1, N2_2, N3_5, N3_6, N4}
    /// Per DPR 642/72 Art. 13
    /// </summary>
    public bool RequiresBollo(Invoice invoice)
    {
        // Case 1: Regime Forfettario — entire imponibile is non-IVA
        if (invoice.IsRegimeForfettario)
        {
            return invoice.ImponibileTotal > BolloThreshold;
        }

        // Case 2: Non-forfettario — check items with NaturaIva codes subject to bollo
        if (invoice.Items != null && invoice.Items.Count > 0)
        {
            decimal nonIvaImponibile = invoice.Items
                .Where(i => i.NaturaIva.HasValue && BolloNaturaIvaCodes.Contains(i.NaturaIva.Value))
                .Sum(i => i.Imponibile);

            if (nonIvaImponibile > BolloThreshold)
            {
                return true;
            }
        }

        return false;
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
