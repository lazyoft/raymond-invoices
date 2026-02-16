using System.Text.RegularExpressions;
using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Validators;

/// <summary>
/// Validator for payment info data per Specifiche FatturaPA, blocco 2.4
/// </summary>
public static class PaymentInfoValidator
{
    /// <summary>
    /// Italian IBAN format: IT + 2 check digits + 1 CIN letter + 5 ABI digits + 5 CAB digits + 12 account chars = 27 chars
    /// </summary>
    private static readonly Regex ItalianIbanRegex = new(@"^IT\d{2}[A-Z]\d{10}[A-Za-z0-9]{12}$", RegexOptions.IgnoreCase);

    /// <summary>
    /// Validates payment info data.
    /// Returns errors (hard failures) and warnings (soft issues that should be flagged but don't block).
    /// </summary>
    public static (bool IsValid, List<string> Errors, List<string> Warnings) Validate(PaymentInfo paymentInfo)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Condizioni must be a valid enum value
        if (!Enum.IsDefined(typeof(PaymentCondition), paymentInfo.Condizioni))
        {
            errors.Add("Condizioni di pagamento non valide");
        }

        // Modalita must be a valid enum value
        if (!Enum.IsDefined(typeof(PaymentMethod), paymentInfo.Modalita))
        {
            errors.Add("Modalità di pagamento non valida");
        }

        // IBAN validation: if provided, must be valid Italian IBAN format
        if (!string.IsNullOrEmpty(paymentInfo.IBAN))
        {
            if (!ItalianIbanRegex.IsMatch(paymentInfo.IBAN))
            {
                errors.Add("IBAN non valido: deve essere un IBAN italiano di 27 caratteri (es. IT60X0542811101000000123456)");
            }
        }

        // If payment method is Bonifico (MP05), IBAN should be provided (warning, not error)
        if (paymentInfo.Modalita == PaymentMethod.MP05_Bonifico && string.IsNullOrEmpty(paymentInfo.IBAN))
        {
            warnings.Add("IBAN non specificato per pagamento con bonifico (MP05): si consiglia di indicare l'IBAN del beneficiario");
        }

        return (errors.Count == 0, errors, warnings);
    }
}
