using System.Text.RegularExpressions;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Implementation of invoice numbering logic
/// </summary>
public class InvoiceNumberingService : IInvoiceNumberingService
{
    private static readonly Regex InvoiceNumberRegex = new(@"^(\d{4})/(\d{3})$");

    /// <summary>
    /// Generates the next invoice number in format YYYY/NNN
    /// </summary>
    public string GenerateNextInvoiceNumber(string? lastInvoiceNumber)
    {
        int currentYear = DateTime.Now.Year;

        if (string.IsNullOrEmpty(lastInvoiceNumber))
        {
            // First invoice ever
            return $"{currentYear}/001";
        }

        if (!ValidateInvoiceNumberFormat(lastInvoiceNumber))
        {
            throw new ArgumentException($"Invalid invoice number format: {lastInvoiceNumber}");
        }

        int lastYear = GetYearFromInvoiceNumber(lastInvoiceNumber);
        int lastSequence = GetSequenceFromInvoiceNumber(lastInvoiceNumber);

        int nextSequence = lastSequence + 1;

        return $"{currentYear}/{nextSequence:D3}";
    }

    /// <summary>
    /// Validates invoice number format (YYYY/NNN)
    /// </summary>
    public bool ValidateInvoiceNumberFormat(string invoiceNumber)
    {
        if (string.IsNullOrEmpty(invoiceNumber))
            return false;

        return InvoiceNumberRegex.IsMatch(invoiceNumber);
    }

    /// <summary>
    /// Extracts year from invoice number
    /// </summary>
    public int GetYearFromInvoiceNumber(string invoiceNumber)
    {
        var match = InvoiceNumberRegex.Match(invoiceNumber);
        if (!match.Success)
            throw new ArgumentException($"Invalid invoice number format: {invoiceNumber}");

        return int.Parse(match.Groups[1].Value);
    }

    /// <summary>
    /// Extracts sequence number from invoice number
    /// </summary>
    public int GetSequenceFromInvoiceNumber(string invoiceNumber)
    {
        var match = InvoiceNumberRegex.Match(invoiceNumber);
        if (!match.Success)
            throw new ArgumentException($"Invalid invoice number format: {invoiceNumber}");

        return int.Parse(match.Groups[2].Value);
    }
}
