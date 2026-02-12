namespace Fatturazione.Domain.Services;

/// <summary>
/// Service for generating and validating invoice numbers
/// </summary>
public interface IInvoiceNumberingService
{
    /// <summary>
    /// Generates the next invoice number in format YYYY/NNN
    /// </summary>
    string GenerateNextInvoiceNumber(string? lastInvoiceNumber);

    /// <summary>
    /// Validates invoice number format
    /// </summary>
    bool ValidateInvoiceNumberFormat(string invoiceNumber);

    /// <summary>
    /// Extracts year from invoice number
    /// </summary>
    int GetYearFromInvoiceNumber(string invoiceNumber);

    /// <summary>
    /// Extracts sequence number from invoice number
    /// </summary>
    int GetSequenceFromInvoiceNumber(string invoiceNumber);
}
