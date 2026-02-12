using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Implementation of invoice calculation logic
/// </summary>
public class InvoiceCalculationService : IInvoiceCalculationService
{
    private readonly IRitenutaService _ritenutaService;

    public InvoiceCalculationService(IRitenutaService ritenutaService)
    {
        _ritenutaService = ritenutaService;
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
            invoice.TotalDue = 0;
            return;
        }

        // Calculate each item's totals
        foreach (var item in invoice.Items)
        {
            CalculateItemTotals(item);
        }

        invoice.ImponibileTotal = invoice.Items.Sum(i => i.Imponibile);
        invoice.IvaTotal = invoice.Items.Sum(i => i.IvaAmount);
        invoice.SubTotal = invoice.ImponibileTotal + invoice.IvaTotal;

        // Calculate IVA breakdown by rate
        invoice.IvaByRate = CalculateIvaByRate(invoice);

        // Calculate ritenuta
        invoice.RitenutaAmount = CalculateRitenutaAmount(invoice);

        // Calculate total due
        invoice.TotalDue = invoice.SubTotal - invoice.RitenutaAmount;
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
            invoice.SubTotal,
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
