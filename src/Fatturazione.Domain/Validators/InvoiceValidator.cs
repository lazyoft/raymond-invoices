using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Validators;

/// <summary>
/// Validator for invoice data
/// </summary>
public static class InvoiceValidator
{
    /// <summary>
    /// NaturaIva codes that represent Reverse Charge (inversione contabile) operations
    /// </summary>
    private static readonly HashSet<NaturaIva> ReverseChargeNaturaCodes = new()
    {
        NaturaIva.N6_1, NaturaIva.N6_2, NaturaIva.N6_3,
        NaturaIva.N6_4, NaturaIva.N6_5, NaturaIva.N6_6,
        NaturaIva.N6_7, NaturaIva.N6_8, NaturaIva.N6_9
    };

    /// <summary>
    /// Validates an invoice
    /// </summary>
    public static (bool IsValid, List<string> Errors) Validate(Invoice invoice)
    {
        var errors = new List<string>();

        // Validate basic fields
        if (invoice.ClientId == Guid.Empty)
        {
            errors.Add("ClientId è obbligatorio");
        }

        if (invoice.InvoiceDate == default)
        {
            errors.Add("InvoiceDate è obbligatoria");
        }

        if (invoice.DueDate == default)
        {
            errors.Add("DueDate è obbligatoria");
        }

        if (invoice.DueDate < invoice.InvoiceDate)
        {
            errors.Add("DueDate deve essere dopo InvoiceDate");
        }

        // Validate items - at least one item required
        if (invoice.Items == null || invoice.Items.Count == 0)
        {
            errors.Add("La fattura deve contenere almeno un item");
        }
        else
        {
            for (int i = 0; i < invoice.Items.Count; i++)
            {
                var item = invoice.Items[i];
                var itemErrors = ValidateInvoiceItem(item, i + 1);
                errors.AddRange(itemErrors);
            }
        }

        // Gap 10.2 - Data fattura non nel futuro
        if (invoice.InvoiceDate != default && invoice.InvoiceDate.Date > DateTime.Today)
        {
            errors.Add("InvoiceDate non può essere nel futuro");
        }

        // Gap 11 - Forfettario: Causale obbligatoria con testo normativo
        // Legge 190/2014, commi 54-89
        if (invoice.IsRegimeForfettario)
        {
            if (string.IsNullOrWhiteSpace(invoice.Causale))
            {
                errors.Add("Per il regime forfettario, la Causale è obbligatoria e deve contenere il riferimento normativo (art. 1, commi 54-89, Legge n. 190/2014)");
            }
            else
            {
                var causaleUpper = invoice.Causale.ToUpperInvariant();
                bool hasArticleRef = causaleUpper.Contains("ART. 1, COMMI 54-89");
                bool hasLawRef = causaleUpper.Contains("LEGGE") && causaleUpper.Contains("190/2014");
                if (!hasArticleRef || !hasLawRef)
                {
                    errors.Add("Per il regime forfettario, la Causale deve contenere il riferimento normativo: \"art. 1, commi 54-89\" e \"Legge ... 190/2014\"");
                }
            }
        }

        // Gap 1.3 - Fattura semplificata: limite 400 EUR (Art. 21-bis DPR 633/72)
        // Eccezione: forfettari senza limite dal 01/01/2025
        if (invoice.IsSimplified && !invoice.IsRegimeForfettario)
        {
            var total = invoice.Items != null
                ? invoice.Items.Sum(i => i.Quantity * i.UnitPrice)
                : 0m;
            if (total > 400m)
            {
                errors.Add("La fattura semplificata non può superare 400 EUR (Art. 21-bis DPR 633/72). Eccezione: regime forfettario senza limite dal 01/01/2025");
            }
        }

        // Gap 13 - Termini di emissione (Art. 21, comma 4, DPR 633/72)
        if (invoice.DataOperazione.HasValue && invoice.InvoiceDate != default)
        {
            var dataOp = invoice.DataOperazione.Value.Date;
            var invoiceDate = invoice.InvoiceDate.Date;

            if (invoice.DocumentType == DocumentType.TD01)
            {
                // Fattura immediata: emissione entro 12 giorni dall'operazione
                var deadline = dataOp.AddDays(12);
                if (invoiceDate > deadline)
                {
                    errors.Add("Per fattura immediata (TD01), la data fattura deve essere entro 12 giorni dalla data operazione (Art. 21, comma 4, DPR 633/72)");
                }
            }
            else if (invoice.DocumentType == DocumentType.TD24)
            {
                // Fattura differita: emissione entro il 15 del mese successivo all'operazione
                var nextMonth = dataOp.AddMonths(1);
                var deadline = new DateTime(nextMonth.Year, nextMonth.Month, 15);
                if (invoiceDate > deadline)
                {
                    errors.Add("Per fattura differita (TD24), la data fattura deve essere entro il 15 del mese successivo alla data operazione (Art. 21, comma 4, lett. a, DPR 633/72)");
                }
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Validates a single invoice item
    /// </summary>
    private static List<string> ValidateInvoiceItem(InvoiceItem item, int itemNumber)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(item.Description))
        {
            errors.Add($"Item {itemNumber}: Description è obbligatoria");
        }

        if (item.Quantity <= 0)
        {
            errors.Add($"Item {itemNumber}: Quantity deve essere maggiore di 0");
        }

        if (item.UnitPrice < 0)
        {
            errors.Add($"Item {itemNumber}: UnitPrice non può essere negativo");
        }

        // Gap 10.2 - NaturaIva obbligatoria quando IvaRate == Zero
        // Specifiche FatturaPA, campo 2.2.1.14
        if (item.IvaRate == IvaRate.Zero && item.NaturaIva == null)
        {
            errors.Add($"Item {itemNumber}: NaturaIva è obbligatoria quando IvaRate è Zero (Specifiche FatturaPA, campo 2.2.1.14)");
        }

        // Gap 10.2 - NaturaIva deve essere null quando IvaRate != Zero
        if (item.IvaRate != IvaRate.Zero && item.NaturaIva != null)
        {
            errors.Add($"Item {itemNumber}: NaturaIva deve essere null quando IvaRate non è Zero");
        }

        // Gap 2.3 - Reverse Charge: se NaturaIva è N6_x, IvaRate deve essere Zero
        // Art. 17 DPR 633/72
        if (item.NaturaIva.HasValue && ReverseChargeNaturaCodes.Contains(item.NaturaIva.Value) && item.IvaRate != IvaRate.Zero)
        {
            errors.Add($"Item {itemNumber}: Per operazioni in reverse charge (N6_x), IvaRate deve essere Zero (Art. 17 DPR 633/72)");
        }

        return errors;
    }

    /// <summary>
    /// Validates invoice status transition
    /// </summary>
    public static bool CanTransitionTo(InvoiceStatus currentStatus, InvoiceStatus newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            (InvoiceStatus.Draft, InvoiceStatus.Issued) => true,
            (InvoiceStatus.Draft, InvoiceStatus.Cancelled) => true,
            (InvoiceStatus.Issued, InvoiceStatus.Sent) => true,
            (InvoiceStatus.Issued, InvoiceStatus.Cancelled) => true,
            (InvoiceStatus.Sent, InvoiceStatus.Paid) => true,
            (InvoiceStatus.Sent, InvoiceStatus.Overdue) => true,
            (InvoiceStatus.Sent, InvoiceStatus.Cancelled) => true,
            (InvoiceStatus.Overdue, InvoiceStatus.Paid) => true,
            (InvoiceStatus.Overdue, InvoiceStatus.Cancelled) => true,
            _ => false
        };
    }
}
