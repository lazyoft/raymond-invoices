using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Service for generating FatturaPA XML (tracciato v1.2.2)
/// Conforme a DL 119/2018, DL 127/2015, Provvedimento AdE 89757/2018
/// </summary>
public interface IFatturaPAXmlService
{
    /// <summary>
    /// Generates FatturaPA XML from an Invoice and the IssuerProfile.
    /// The generated XML follows the FatturaPA 1.2.2 schema.
    /// </summary>
    /// <param name="invoice">The invoice to convert to XML (must include Client navigation property)</param>
    /// <param name="issuer">The issuer profile (cedente/prestatore)</param>
    /// <returns>The FatturaPA XML as a string</returns>
    string GenerateXml(Invoice invoice, IssuerProfile issuer);
}
