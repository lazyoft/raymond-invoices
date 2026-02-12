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
    /// Calculates ritenuta amount
    /// </summary>
    decimal CalculateRitenuta(decimal imponibile, decimal percentage);

    /// <summary>
    /// Gets standard ritenuta rate for client type
    /// </summary>
    decimal GetStandardRate(ClientType clientType);
}
