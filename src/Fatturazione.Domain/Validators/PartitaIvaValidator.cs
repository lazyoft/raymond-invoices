namespace Fatturazione.Domain.Validators;

/// <summary>
/// Validator for Italian Partita IVA (VAT numbers)
/// Implements the official checksum algorithm
/// </summary>
public static class PartitaIvaValidator
{
    /// <summary>
    /// Validates an Italian Partita IVA
    /// Must be 11 digits and pass the checksum algorithm
    /// </summary>
    public static bool Validate(string partitaIva)
    {
        if (string.IsNullOrWhiteSpace(partitaIva))
            return false;

        // Remove any spaces or dashes
        partitaIva = partitaIva.Replace(" ", "").Replace("-", "");

        // Must be exactly 11 digits
        if (partitaIva.Length != 11)
            return false;

        // Must be all digits
        if (!partitaIva.All(char.IsDigit))
            return false;

        // Apply checksum algorithm
        return ValidateChecksum(partitaIva);
    }

    /// <summary>
    /// Validates the checksum using the official Italian algorithm
    /// </summary>
    private static bool ValidateChecksum(string partitaIva)
    {
        int sum = 0;

        // Process first 10 digits
        for (int i = 0; i < 10; i++)
        {
            int digit = int.Parse(partitaIva[i].ToString());

            if (i % 2 == 0)
            {
                // Even position (0, 2, 4, 6, 8)
                sum += digit;
            }
            else
            {
                // Odd position (1, 3, 5, 7, 9)
                int doubled = digit * 2;
                if (doubled > 9)
                    doubled -= 9;
                sum += doubled;
            }
        }

        // Calculate expected check digit
        int checkDigit = (10 - (sum % 10)) % 10;

        // Compare with actual last digit
        int actualCheckDigit = int.Parse(partitaIva[10].ToString());

        return checkDigit == actualCheckDigit;
    }

    /// <summary>
    /// Gets validation error message
    /// </summary>
    public static string GetValidationError(string partitaIva)
    {
        if (string.IsNullOrWhiteSpace(partitaIva))
            return "Partita IVA è obbligatoria";

        partitaIva = partitaIva.Replace(" ", "").Replace("-", "");

        if (partitaIva.Length != 11)
            return "Partita IVA deve essere di 11 cifre";

        if (!partitaIva.All(char.IsDigit))
            return "Partita IVA deve contenere solo numeri";

        if (!ValidateChecksum(partitaIva))
            return "Partita IVA non valida (checksum errato)";

        return string.Empty;
    }
}
