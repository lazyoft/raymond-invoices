namespace Fatturazione.Domain.Models;

/// <summary>
/// Invoice lifecycle status
/// </summary>
public enum InvoiceStatus
{
    /// <summary>
    /// Bozza - Invoice is being edited, not yet issued
    /// </summary>
    Draft,

    /// <summary>
    /// Emessa - Invoice has been issued with a number and date
    /// </summary>
    Issued,

    /// <summary>
    /// Inviata - Invoice has been sent to the client
    /// </summary>
    Sent,

    /// <summary>
    /// Pagata - Invoice has been paid
    /// </summary>
    Paid,

    /// <summary>
    /// Scaduta - Invoice is past its due date and unpaid
    /// </summary>
    Overdue,

    /// <summary>
    /// Annullata - Invoice has been cancelled
    /// </summary>
    Cancelled
}
