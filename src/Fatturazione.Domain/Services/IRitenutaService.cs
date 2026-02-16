using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Service for calculating Ritenuta d'Acconto (withholding tax)
/// </summary>
public interface IRitenutaService
{
    /// <summary>
    /// Determines if ritenuta applies to a client
    /// </summary>
    bool AppliesRitenuta(Client client);

    /// <summary>
    /// Calculates ritenuta amount using a flat percentage.
    /// Kept for backward compatibility.
    /// </summary>
    decimal CalculateRitenuta(decimal imponibile, decimal percentage);

    /// <summary>
    /// Calculates ritenuta amount using the client's ritenuta configuration
    /// (TipoRitenuta, RitenutaPercentage, RitenutaBaseCalcoloPercentuale).
    /// Formula: imponibile × (BaseCalcoloPercentuale / 100) × (AliquotaRitenuta / 100)
    /// Art. 25 DPR 600/73, Art. 25-bis DPR 600/73
    /// </summary>
    decimal CalculateRitenuta(decimal imponibile, Client client);

    /// <summary>
    /// Gets standard ritenuta rate for client type
    /// </summary>
    decimal GetStandardRate(ClientType clientType);
}
