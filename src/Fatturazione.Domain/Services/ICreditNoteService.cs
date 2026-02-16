using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Service for creating and validating Credit Notes (TD04) and Debit Notes (TD05)
/// as per Art. 26 DPR 633/72.
/// </summary>
public interface ICreditNoteService
{
    /// <summary>
    /// Creates a credit note (Nota di Credito, TD04) from an existing invoice.
    /// Copies all items from the original invoice with negated amounts.
    /// Art. 26, comma 2, DPR 633/72 — variazione in diminuzione.
    /// </summary>
    /// <param name="originalInvoice">The original invoice to credit</param>
    /// <param name="reason">The reason for issuing the credit note (causale)</param>
    /// <returns>A new Invoice with DocumentType=TD04, status=Draft</returns>
    Invoice CreateCreditNote(Invoice originalInvoice, string reason);

    /// <summary>
    /// Creates a debit note (Nota di Debito, TD05) for additional charges on an existing invoice.
    /// Art. 26, comma 1, DPR 633/72 — variazione in aumento.
    /// </summary>
    /// <param name="originalInvoice">The original invoice to reference</param>
    /// <param name="additionalItems">The additional line items for the debit note</param>
    /// <param name="reason">The reason for issuing the debit note (causale)</param>
    /// <returns>A new Invoice with DocumentType=TD05, status=Draft</returns>
    Invoice CreateDebitNote(Invoice originalInvoice, List<InvoiceItem> additionalItems, string reason);

    /// <summary>
    /// Validates a credit or debit note against fiscal rules.
    /// Checks: DocumentType is TD04/TD05, RelatedInvoiceId/Number set,
    /// original invoice is in non-Draft status, amounts do not exceed original.
    /// </summary>
    /// <param name="creditNote">The credit/debit note to validate</param>
    /// <param name="originalInvoice">The original invoice (may be null if not found)</param>
    /// <returns>Validation result with any errors</returns>
    (bool IsValid, List<string> Errors) ValidateCreditNote(Invoice creditNote, Invoice? originalInvoice);
}
