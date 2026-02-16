namespace Fatturazione.Domain.Models;

/// <summary>
/// Profilo dell'emittente (cedente/prestatore) — Art. 21, co. 2, lett. c-d, DPR 633/72
/// Dati obbligatori su ogni fattura
/// </summary>
public class IssuerProfile
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Denominazione o Ragione Sociale
    /// </summary>
    public string RagioneSociale { get; set; } = string.Empty;

    /// <summary>
    /// Sede legale / domicilio
    /// </summary>
    public Address Indirizzo { get; set; } = new();

    /// <summary>
    /// Partita IVA dell'emittente (11 cifre)
    /// </summary>
    public string PartitaIva { get; set; } = string.Empty;

    /// <summary>
    /// Codice Fiscale dell'emittente
    /// </summary>
    public string? CodiceFiscale { get; set; }

    /// <summary>
    /// Regime fiscale (RF01 = ordinario, RF19 = forfettario, ecc.)
    /// </summary>
    public string RegimeFiscale { get; set; } = "RF01";

    /// <summary>
    /// Telefono
    /// </summary>
    public string? Telefono { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }
}
