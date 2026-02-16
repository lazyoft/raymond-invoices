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
    /// Whether this client is subject to Split Payment (scissione dei pagamenti)
    /// Typically true for Public Administration clients
    /// Per Art. 17-ter DPR 633/72
    /// </summary>
    public bool SubjectToSplitPayment { get; set; }

    /// <summary>
    /// Codice Univoco Ufficio - 6 alphanumeric characters, mandatory for PA
    /// </summary>
    public string? CodiceUnivocoUfficio { get; set; }

    /// <summary>
    /// CIG - Codice Identificativo di Gara (10 characters)
    /// </summary>
    public string? CIG { get; set; }

    /// <summary>
    /// CUP - Codice Unico di Progetto (15 characters)
    /// </summary>
    public string? CUP { get; set; }

    /// <summary>
    /// Codice Destinatario SDI — 7 caratteri alfanumerici per B2B, "0000000" se PEC
    /// Per PA usa CodiceUnivocoUfficio (6 caratteri)
    /// </summary>
    public string? CodiceDestinatario { get; set; }

    /// <summary>
    /// Indirizzo PEC del destinatario (alternativa al Codice Destinatario)
    /// </summary>
    public string? PEC { get; set; }

    /// <summary>
    /// Tipo ritenuta (RT01 persone fisiche, RT02 persone giuridiche)
    /// </summary>
    public TipoRitenuta TipoRitenuta { get; set; } = TipoRitenuta.RT01;

    /// <summary>
    /// Percentuale base di calcolo per ritenuta (100% per professionisti, 50% per agenti senza dipendenti, 20% per agenti con dipendenti)
    /// </summary>
    public decimal RitenutaBaseCalcoloPercentuale { get; set; } = 100m;

    /// <summary>
    /// Causale pagamento per CU (A = lavoro autonomo, Q = agente monomandatario, R = agente plurimandatario, ecc.)
    /// </summary>
    public CausalePagamento? CausalePagamento { get; set; }

    /// <summary>
    /// Date the client was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional notes about the client
    /// </summary>
    public string? Notes { get; set; }
}
