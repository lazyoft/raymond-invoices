namespace Fatturazione.Domain.Models;

/// <summary>
/// Type of client for Italian tax purposes
/// </summary>
public enum ClientType
{
    /// <summary>
    /// Professionista - Professional/freelancer (subject to ritenuta d'acconto)
    /// Examples: lawyers, consultants, designers, developers
    /// </summary>
    Professional,

    /// <summary>
    /// Società - Company/corporation (not subject to ritenuta d'acconto)
    /// Examples: SRL, SPA, SRLS
    /// </summary>
    Company,

    /// <summary>
    /// Pubblica Amministrazione - Public administration (subject to split payment)
    /// Examples: municipalities, government agencies, public schools
    /// </summary>
    PublicAdministration
}
