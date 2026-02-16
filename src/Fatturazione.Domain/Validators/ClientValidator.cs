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
    private static readonly Regex CigRegex = new(@"^[A-Za-z0-9]{10}$");
    private static readonly Regex CupRegex = new(@"^[A-Za-z0-9]{15}$");
    private static readonly Regex CodiceDestinatarioRegex = new(@"^[A-Za-z0-9]{7}$");
    private static readonly Regex PecRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

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

        // CodiceFiscale - optional but if provided must be valid format and checksum
        if (!string.IsNullOrEmpty(client.CodiceFiscale))
        {
            if (CodiceFiscalePersonaFisicaRegex.IsMatch(client.CodiceFiscale))
            {
                // Format is valid for persona fisica — now verify checksum
                if (!CodiceFiscaleValidator.ValidateChecksum(client.CodiceFiscale))
                {
                    errors.Add("Codice Fiscale non valido (carattere di controllo errato)");
                }
            }
            else if (!CodiceFiscalePersonaGiuridicaRegex.IsMatch(client.CodiceFiscale))
            {
                errors.Add("CodiceFiscale deve essere 16 caratteri alfanumerici (persona fisica) o 11 cifre (persona giuridica)");
            }
        }

        // CIG - optional but if provided must be 10 alphanumeric characters (Art. 25 DL 66/2014)
        if (!string.IsNullOrEmpty(client.CIG) && !CigRegex.IsMatch(client.CIG))
        {
            errors.Add("CIG deve essere esattamente 10 caratteri alfanumerici");
        }

        // CUP - optional but if provided must be 15 alphanumeric characters (Legge 136/2010)
        if (!string.IsNullOrEmpty(client.CUP) && !CupRegex.IsMatch(client.CUP))
        {
            errors.Add("CUP deve essere esattamente 15 caratteri alfanumerici");
        }

        // CodiceDestinatario - optional but if provided must be 7 alphanumeric characters for non-PA (B2B)
        if (!string.IsNullOrEmpty(client.CodiceDestinatario))
        {
            if (!CodiceDestinatarioRegex.IsMatch(client.CodiceDestinatario))
            {
                errors.Add("CodiceDestinatario deve essere esattamente 7 caratteri alfanumerici");
            }
        }

        // PEC - optional but if provided must be valid email format
        if (!string.IsNullOrEmpty(client.PEC) && !PecRegex.IsMatch(client.PEC))
        {
            errors.Add("PEC non è in un formato email valido");
        }

        // Split payment: only valid for PublicAdministration (Art. 17-ter DPR 633/72)
        if (client.SubjectToSplitPayment && client.ClientType != ClientType.PublicAdministration)
        {
            errors.Add("Split payment (scissione dei pagamenti) è applicabile solo a clienti di tipo Pubblica Amministrazione");
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
