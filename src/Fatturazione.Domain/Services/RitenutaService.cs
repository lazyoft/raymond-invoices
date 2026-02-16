using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Implementation of Ritenuta d'Acconto calculations
/// </summary>
public class RitenutaService : IRitenutaService
{
    /// <summary>
    /// Determines if ritenuta applies to a client
    /// Ritenuta typically applies to professionals (freelancers, consultants)
    /// but not to companies or public administration
    /// </summary>
    public bool AppliesRitenuta(Client client)
    {
        return client.SubjectToRitenuta;
    }

    /// <summary>
    /// Calculates ritenuta amount based on imponibile (taxable amount)
    /// Ritenuta is always calculated on the imponibile, NOT on the total with IVA
    /// Kept for backward compatibility.
    /// </summary>
    /// <param name="imponibile">Taxable amount (pre-VAT)</param>
    /// <param name="percentage">Ritenuta percentage (typically 20%)</param>
    /// <returns>Ritenuta amount</returns>
    public decimal CalculateRitenuta(decimal imponibile, decimal percentage)
    {
        return Math.Round(imponibile * (percentage / 100m), 2);
    }

    /// <summary>
    /// Calculates ritenuta amount using the client's ritenuta configuration.
    /// Uses the formula: imponibile × (BaseCalcoloPercentuale / 100) × (AliquotaRitenuta / 100)
    /// Art. 25 DPR 600/73 (professionisti, occasionali, non residenti)
    /// Art. 25-bis DPR 600/73 (agenti e rappresentanti di commercio)
    /// </summary>
    /// <param name="imponibile">Taxable amount (pre-VAT)</param>
    /// <param name="client">Client with ritenuta configuration</param>
    /// <returns>Ritenuta amount, or 0 if the client is not subject to ritenuta</returns>
    public decimal CalculateRitenuta(decimal imponibile, Client client)
    {
        if (!client.SubjectToRitenuta)
            return 0m;

        return Math.Round(
            imponibile
            * (client.RitenutaBaseCalcoloPercentuale / 100m)
            * (client.RitenutaPercentage / 100m),
            2);
    }

    /// <summary>
    /// Gets standard ritenuta rate for client type
    /// </summary>
    public decimal GetStandardRate(ClientType clientType)
    {
        return clientType switch
        {
            ClientType.Professional => 20.0m,
            ClientType.Company => 0.0m,
            ClientType.PublicAdministration => 0.0m,
            _ => 0.0m
        };
    }
}
