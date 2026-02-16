using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Implementation of credit note (TD04) and debit note (TD05) operations.
/// Art. 26 DPR 633/72 — Variazioni dell'imponibile o dell'imposta.
/// </summary>
public class CreditNoteService : ICreditNoteService
{
    /// <summary>
    /// Valid statuses for an original invoice that can be referenced by a credit/debit note.
    /// A Draft invoice has no fiscal value and cannot be credited or debited.
    /// </summary>
    private static readonly HashSet<InvoiceStatus> ValidOriginalStatuses = new()
    {
        InvoiceStatus.Issued,
        InvoiceStatus.Sent,
        InvoiceStatus.Paid,
        InvoiceStatus.Overdue
    };

    /// <inheritdoc />
    public Invoice CreateCreditNote(Invoice originalInvoice, string reason)
    {
        var creditNote = new Invoice
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.TD04,
            Status = InvoiceStatus.Draft,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            ClientId = originalInvoice.ClientId,
            Client = originalInvoice.Client,
            RelatedInvoiceId = originalInvoice.Id,
            RelatedInvoiceNumber = originalInvoice.InvoiceNumber,
            Notes = reason,
            Causale = reason,
            IsRegimeForfettario = originalInvoice.IsRegimeForfettario,
            IssuerProfileId = originalInvoice.IssuerProfileId,
            IssuerProfile = originalInvoice.IssuerProfile,
            Items = new List<InvoiceItem>()
        };

        foreach (var originalItem in originalInvoice.Items)
        {
            var creditItem = new InvoiceItem
            {
                Id = Guid.NewGuid(),
                Description = originalItem.Description,
                Quantity = originalItem.Quantity,
                UnitPrice = -Math.Abs(originalItem.UnitPrice),
                IvaRate = originalItem.IvaRate,
                NaturaIva = originalItem.NaturaIva,
                DiscountPercentage = originalItem.DiscountPercentage,
                DiscountAmount = originalItem.DiscountAmount,
                Imponibile = -Math.Abs(originalItem.Imponibile),
                IvaAmount = -Math.Abs(originalItem.IvaAmount),
                Total = -Math.Abs(originalItem.Total)
            };

            creditNote.Items.Add(creditItem);
        }

        // Set negated totals from original invoice
        creditNote.ImponibileTotal = -Math.Abs(originalInvoice.ImponibileTotal);
        creditNote.IvaTotal = -Math.Abs(originalInvoice.IvaTotal);
        creditNote.SubTotal = -Math.Abs(originalInvoice.SubTotal);
        creditNote.RitenutaAmount = -Math.Abs(originalInvoice.RitenutaAmount);
        creditNote.BolloAmount = 0m; // Bollo is not duplicated on credit notes
        creditNote.TotalDue = -Math.Abs(originalInvoice.TotalDue);

        return creditNote;
    }

    /// <inheritdoc />
    public Invoice CreateDebitNote(Invoice originalInvoice, List<InvoiceItem> additionalItems, string reason)
    {
        var debitNote = new Invoice
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.TD05,
            Status = InvoiceStatus.Draft,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            ClientId = originalInvoice.ClientId,
            Client = originalInvoice.Client,
            RelatedInvoiceId = originalInvoice.Id,
            RelatedInvoiceNumber = originalInvoice.InvoiceNumber,
            Notes = reason,
            Causale = reason,
            IsRegimeForfettario = originalInvoice.IsRegimeForfettario,
            IssuerProfileId = originalInvoice.IssuerProfileId,
            IssuerProfile = originalInvoice.IssuerProfile,
            Items = new List<InvoiceItem>()
        };

        foreach (var item in additionalItems)
        {
            var debitItem = new InvoiceItem
            {
                Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                IvaRate = item.IvaRate,
                NaturaIva = item.NaturaIva,
                DiscountPercentage = item.DiscountPercentage,
                DiscountAmount = item.DiscountAmount,
                Imponibile = item.Imponibile,
                IvaAmount = item.IvaAmount,
                Total = item.Total
            };

            debitNote.Items.Add(debitItem);
        }

        // Sum the totals from additional items
        debitNote.ImponibileTotal = debitNote.Items.Sum(i => i.Imponibile);
        debitNote.IvaTotal = debitNote.Items.Sum(i => i.IvaAmount);
        debitNote.SubTotal = debitNote.ImponibileTotal + debitNote.IvaTotal;
        debitNote.TotalDue = debitNote.SubTotal;

        return debitNote;
    }

    /// <inheritdoc />
    public (bool IsValid, List<string> Errors) ValidateCreditNote(Invoice creditNote, Invoice? originalInvoice)
    {
        var errors = new List<string>();

        // 1. DocumentType must be TD04 or TD05
        if (creditNote.DocumentType != DocumentType.TD04 && creditNote.DocumentType != DocumentType.TD05)
        {
            errors.Add("Il tipo documento deve essere TD04 (nota di credito) o TD05 (nota di debito).");
        }

        // 2. RelatedInvoiceId must be set
        if (!creditNote.RelatedInvoiceId.HasValue || creditNote.RelatedInvoiceId == Guid.Empty)
        {
            errors.Add("Il riferimento alla fattura originaria (RelatedInvoiceId) e obbligatorio per le note di credito/debito (Art. 26 DPR 633/72).");
        }

        // 3. RelatedInvoiceNumber must be set
        if (string.IsNullOrWhiteSpace(creditNote.RelatedInvoiceNumber))
        {
            errors.Add("Il numero della fattura originaria (RelatedInvoiceNumber) e obbligatorio per le note di credito/debito (Art. 26 DPR 633/72).");
        }

        // 4. Original invoice must exist and be in a non-Draft status
        if (originalInvoice == null)
        {
            errors.Add("La fattura originaria non esiste.");
        }
        else
        {
            if (!ValidOriginalStatuses.Contains(originalInvoice.Status))
            {
                errors.Add($"La fattura originaria e in stato '{originalInvoice.Status}'. Solo fatture emesse (Issued, Sent, Paid, Overdue) possono essere oggetto di nota di credito/debito.");
            }

            // 5. For credit notes (TD04), the absolute amounts should not exceed the original
            if (creditNote.DocumentType == DocumentType.TD04)
            {
                decimal creditAbsImponibile = Math.Abs(creditNote.ImponibileTotal);
                decimal originalAbsImponibile = Math.Abs(originalInvoice.ImponibileTotal);

                if (creditAbsImponibile > originalAbsImponibile)
                {
                    errors.Add($"L'imponibile della nota di credito ({creditAbsImponibile:F2} EUR) supera l'imponibile della fattura originaria ({originalAbsImponibile:F2} EUR). Art. 26 DPR 633/72 non consente note di credito superiori all'importo originario.");
                }

                decimal creditAbsIva = Math.Abs(creditNote.IvaTotal);
                decimal originalAbsIva = Math.Abs(originalInvoice.IvaTotal);

                if (creditAbsIva > originalAbsIva)
                {
                    errors.Add($"L'IVA della nota di credito ({creditAbsIva:F2} EUR) supera l'IVA della fattura originaria ({originalAbsIva:F2} EUR).");
                }
            }
        }

        return (errors.Count == 0, errors);
    }
}
