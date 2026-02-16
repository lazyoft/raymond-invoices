using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Implementation of invoice calculation logic
/// </summary>
public class InvoiceCalculationService : IInvoiceCalculationService
{
    private readonly IRitenutaService _ritenutaService;
    private readonly IBolloService _bolloService;

    public InvoiceCalculationService(IRitenutaService ritenutaService, IBolloService bolloService)
    {
        _ritenutaService = ritenutaService;
        _bolloService = bolloService;
    }

    /// <summary>
    /// Calculates all totals for an invoice
    /// </summary>
    public void CalculateInvoiceTotals(Invoice invoice)
    {
        if (invoice.Items == null || invoice.Items.Count == 0)
        {
            invoice.ImponibileTotal = 0;
            invoice.IvaTotal = 0;
            invoice.SubTotal = 0;
            invoice.RitenutaAmount = 0;
            invoice.BolloAmount = 0;
            invoice.TotalDue = 0;
            return;
        }

        // Calculate each item's totals
        foreach (var item in invoice.Items)
        {
            CalculateItemTotals(item);
        }

        invoice.ImponibileTotal = invoice.Items.Sum(i => i.Imponibile);

        // Handle Regime Forfettario: no IVA, no ritenuta
        if (invoice.IsRegimeForfettario)
        {
            // Force IVA to 0 for all items (per Legge 190/2014)
            foreach (var item in invoice.Items)
            {
                item.IvaAmount = 0;
                item.Total = item.Imponibile;
            }

            invoice.IvaTotal = 0;
            invoice.SubTotal = invoice.ImponibileTotal;
            invoice.IvaByRate = new Dictionary<IvaRate, decimal>();
            invoice.RitenutaAmount = 0;

            // Calculate stamp duty (imposta di bollo) per DPR 642/72 Art. 13
            invoice.BolloAmount = _bolloService.CalculateBollo(invoice);

            // Total due = Imponibile + Bollo (no IVA, no ritenuta for forfettari)
            invoice.TotalDue = invoice.ImponibileTotal + invoice.BolloAmount;
        }
        else
        {
            // Standard calculation for non-forfettari
            invoice.IvaTotal = invoice.Items.Sum(i => i.IvaAmount);
            invoice.SubTotal = invoice.ImponibileTotal + invoice.IvaTotal;

            // Calculate IVA breakdown by rate
            invoice.IvaByRate = CalculateIvaByRate(invoice);

            // Check for split payment (Art. 17-ter DPR 633/72)
            bool isSplitPayment = invoice.Client?.SubjectToSplitPayment == true;

            if (isSplitPayment)
            {
                // Split payment: IVA goes directly to Treasury, not to supplier
                // Ritenuta does NOT apply with split payment (mutually exclusive)
                invoice.RitenutaAmount = 0;
                invoice.BolloAmount = 0;
                // TotalDue = ImponibileTotal (IVA is not collected by supplier)
                invoice.TotalDue = invoice.ImponibileTotal;
            }
            else
            {
                // Calculate ritenuta
                invoice.RitenutaAmount = CalculateRitenutaAmount(invoice);

                // No bollo for standard invoices with IVA
                invoice.BolloAmount = 0;

                // Calculate total due
                invoice.TotalDue = invoice.SubTotal - invoice.RitenutaAmount;
            }
        }
    }

    /// <summary>
    /// Calculates totals for a single invoice item
    /// </summary>
    public void CalculateItemTotals(InvoiceItem item)
    {
        // Calculate subtotal
        decimal subtotal = item.Quantity * item.UnitPrice;

        // Apply discount (stub for future feature)
        decimal discountAmount = 0;
        if (item.DiscountPercentage > 0)
        {
            discountAmount = subtotal * (item.DiscountPercentage / 100m);
        }
        else
        {
            discountAmount = item.DiscountAmount;
        }

        // Calculate imponibile (taxable amount)
        item.Imponibile = subtotal - discountAmount;

        item.IvaAmount = Math.Round(item.Imponibile * ((decimal)item.IvaRate / 100m), 2);

        item.Total = item.Imponibile + item.IvaAmount;
    }

    /// <summary>
    /// Calculates ritenuta amount for an invoice
    /// </summary>
    public decimal CalculateRitenutaAmount(Invoice invoice)
    {
        if (invoice.Client == null || !_ritenutaService.AppliesRitenuta(invoice.Client))
        {
            return 0;
        }

        return _ritenutaService.CalculateRitenuta(
            invoice.ImponibileTotal,
            invoice.Client.RitenutaPercentage
        );
    }

    /// <summary>
    /// Calculates IVA breakdown by rate
    /// </summary>
    public Dictionary<IvaRate, decimal> CalculateIvaByRate(Invoice invoice)
    {
        var ivaByRate = new Dictionary<IvaRate, decimal>();

        foreach (var item in invoice.Items)
        {
            if (!ivaByRate.ContainsKey(item.IvaRate))
            {
                ivaByRate[item.IvaRate] = 0;
            }
            ivaByRate[item.IvaRate] += item.IvaAmount;
        }

        return ivaByRate;
    }
}
