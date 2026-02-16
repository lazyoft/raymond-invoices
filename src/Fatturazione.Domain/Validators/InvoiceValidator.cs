using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Validators;

/// <summary>
/// Validator for invoice data
/// </summary>
public static class InvoiceValidator
{
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
