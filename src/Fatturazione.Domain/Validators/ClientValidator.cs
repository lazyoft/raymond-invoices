using System.Text.RegularExpressions;
using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Validators;

/// <summary>
/// Validator for client data per Art. 21, comma 2, lett. e-f, DPR 633/72
/// </summary>
public static class ClientValidator
{
    private static readonly Regex PostalCodeRegex = new(@"^\d{5}$");
    private static readonly Regex ProvinceRegex = new(@"^[A-Z]{2}$");
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    private static readonly Regex CodiceFiscalePersonaFisicaRegex = new(@"^[A-Z]{6}\d{2}[A-Z]\d{2}[A-Z]\d{3}[A-Z]$", RegexOptions.IgnoreCase);
    private static readonly Regex CodiceFiscalePersonaGiuridicaRegex = new(@"^\d{11}$");
    private static readonly Regex CodiceUnivocoUfficioRegex = new(@"^[A-Z0-9]{6}$", RegexOptions.IgnoreCase);

    /// <summary>
    /// Validates client data
    /// </summary>
    public static (bool IsValid, List<string> Errors) Validate(Client client)
    {
        var errors = new List<string>();

        // RagioneSociale - mandatory
        if (string.IsNullOrWhiteSpace(client.RagioneSociale))
        {
            errors.Add("RagioneSociale è obbligatoria");
        }

        // Email - mandatory and valid format
        if (string.IsNullOrWhiteSpace(client.Email))
        {
            errors.Add("Email è obbligatoria");
        }
        else if (!EmailRegex.IsMatch(client.Email))
        {
            errors.Add("Email non è in un formato valido");
        }

        // CodiceFiscale - optional but if provided must be valid format
        if (!string.IsNullOrEmpty(client.CodiceFiscale))
        {
            if (!CodiceFiscalePersonaFisicaRegex.IsMatch(client.CodiceFiscale) &&
                !CodiceFiscalePersonaGiuridicaRegex.IsMatch(client.CodiceFiscale))
            {
                errors.Add("CodiceFiscale deve essere 16 caratteri alfanumerici (persona fisica) o 11 cifre (persona giuridica)");
            }
        }

        // Address validations
        if (client.Address != null)
        {
            if (string.IsNullOrWhiteSpace(client.Address.Street))
            {
                errors.Add("Address.Street è obbligatorio");
            }

            if (string.IsNullOrWhiteSpace(client.Address.City))
            {
                errors.Add("Address.City è obbligatorio");
            }

            if (!string.IsNullOrEmpty(client.Address.PostalCode) && !PostalCodeRegex.IsMatch(client.Address.PostalCode))
            {
                errors.Add("Address.PostalCode deve essere esattamente 5 cifre numeriche");
            }

            if (!string.IsNullOrEmpty(client.Address.Province) && !ProvinceRegex.IsMatch(client.Address.Province))
            {
                errors.Add("Address.Province deve essere esattamente 2 lettere maiuscole (es. MI, RM)");
            }
        }

        // PA-specific validations
        if (client.ClientType == ClientType.PublicAdministration)
        {
            if (string.IsNullOrEmpty(client.CodiceUnivocoUfficio))
            {
                errors.Add("CodiceUnivocoUfficio è obbligatorio per clienti PA");
            }
            else if (!CodiceUnivocoUfficioRegex.IsMatch(client.CodiceUnivocoUfficio))
            {
                errors.Add("CodiceUnivocoUfficio deve essere 6 caratteri alfanumerici");
            }
        }

        return (errors.Count == 0, errors);
    }
}
