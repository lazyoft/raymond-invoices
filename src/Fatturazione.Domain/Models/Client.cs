namespace Fatturazione.Domain.Models;

/// <summary>
/// Italian client/customer entity
/// Represents a company or professional who receives invoices
/// </summary>
public class Client
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Ragione Sociale - Legal/company name
    /// </summary>
    public string RagioneSociale { get; set; } = string.Empty;

    /// <summary>
    /// Partita IVA - Italian VAT number (11 digits)
    /// </summary>
    public string PartitaIva { get; set; } = string.Empty;

    /// <summary>
    /// Codice Fiscale - Italian tax code (16 characters for individuals)
    /// </summary>
    public string? CodiceFiscale { get; set; }

    /// <summary>
    /// Type of client (Professional, Company, PublicAdministration)
    /// </summary>
    public ClientType ClientType { get; set; }

    /// <summary>
    /// Legal address (sede legale)
    /// </summary>
    public Address Address { get; set; } = new();

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Whether this client is subject to Ritenuta d'Acconto (withholding tax)
    /// Typically true for professionals, false for companies
    /// </summary>
    public bool SubjectToRitenuta { get; set; }

    /// <summary>
    /// Ritenuta percentage (default 20% for professionals)
    /// </summary>
    public decimal RitenutaPercentage { get; set; } = 20.0m;

    /// <summary>
    /// Date the client was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional notes about the client
    /// </summary>
    public string? Notes { get; set; }
}
