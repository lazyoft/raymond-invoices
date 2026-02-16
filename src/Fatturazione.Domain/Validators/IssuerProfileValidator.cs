using System.Text.RegularExpressions;
using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Validators;

/// <summary>
/// Validator for IssuerProfile data per Art. 21, co. 2, lett. c-d, DPR 633/72
/// Validates the issuer (cedente/prestatore) mandatory fields
/// </summary>
public static class IssuerProfileValidator
{
    private static readonly Regex RegimeFiscaleRegex = new(@"^RF(0[1-9]|1[0-9])$");
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    /// <summary>
    /// Validates issuer profile data
    /// </summary>
    public static (bool IsValid, List<string> Errors) Validate(IssuerProfile profile)
    {
        var errors = new List<string>();

        // RagioneSociale - mandatory (Art. 21, co. 2, lett. c, DPR 633/72)
        if (string.IsNullOrWhiteSpace(profile.RagioneSociale))
        {
            errors.Add("RagioneSociale è obbligatoria");
        }

        // PartitaIva - mandatory and must be valid (Art. 21, co. 2, lett. d, DPR 633/72)
        if (string.IsNullOrWhiteSpace(profile.PartitaIva))
        {
            errors.Add("PartitaIva è obbligatoria");
        }
        else if (!PartitaIvaValidator.Validate(profile.PartitaIva))
        {
            var error = PartitaIvaValidator.GetValidationError(profile.PartitaIva);
            errors.Add($"PartitaIva non valida: {error}");
        }

        // Indirizzo - Street and City are mandatory (sede legale)
        if (profile.Indirizzo != null)
        {
            if (string.IsNullOrWhiteSpace(profile.Indirizzo.Street))
            {
                errors.Add("Indirizzo.Street è obbligatorio");
            }

            if (string.IsNullOrWhiteSpace(profile.Indirizzo.City))
            {
                errors.Add("Indirizzo.City è obbligatorio");
            }
        }
        else
        {
            errors.Add("Indirizzo è obbligatorio");
        }

        // RegimeFiscale - must match pattern RF01-RF19
        if (string.IsNullOrWhiteSpace(profile.RegimeFiscale))
        {
            errors.Add("RegimeFiscale è obbligatorio");
        }
        else if (!RegimeFiscaleRegex.IsMatch(profile.RegimeFiscale))
        {
            errors.Add("RegimeFiscale deve essere nel formato RF01-RF19");
        }

        // Email - optional but must be valid format if provided
        if (!string.IsNullOrEmpty(profile.Email) && !EmailRegex.IsMatch(profile.Email))
        {
            errors.Add("Email non è in un formato valido");
        }

        return (errors.Count == 0, errors);
    }
}
