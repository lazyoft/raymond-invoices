using System.Collections.Generic;

namespace Fatturazione.Domain.Validators;

/// <summary>
/// Validator for Italian Codice Fiscale (persona fisica, 16 characters)
/// Implements the official checksum algorithm per DM 12/03/1974
/// </summary>
public static class CodiceFiscaleValidator
{
    /// <summary>
    /// Values for characters in ODD positions (1-based: 1, 3, 5, ..., 15)
    /// </summary>
    private static readonly Dictionary<char, int> OddPositionValues = new()
    {
        ['0'] = 1, ['1'] = 0, ['2'] = 5, ['3'] = 7, ['4'] = 9,
        ['5'] = 13, ['6'] = 15, ['7'] = 17, ['8'] = 19, ['9'] = 21,
        ['A'] = 1, ['B'] = 0, ['C'] = 5, ['D'] = 7, ['E'] = 9,
        ['F'] = 13, ['G'] = 15, ['H'] = 17, ['I'] = 19, ['J'] = 21,
        ['K'] = 2, ['L'] = 4, ['M'] = 18, ['N'] = 20, ['O'] = 11,
        ['P'] = 3, ['Q'] = 6, ['R'] = 8, ['S'] = 12, ['T'] = 14,
        ['U'] = 16, ['V'] = 10, ['W'] = 22, ['X'] = 25, ['Y'] = 24,
        ['Z'] = 23
    };

    /// <summary>
    /// Values for characters in EVEN positions (1-based: 2, 4, 6, ..., 14)
    /// Letters: A=0, B=1, ..., Z=25. Digits: 0=0, 1=1, ..., 9=9.
    /// </summary>
    private static readonly Dictionary<char, int> EvenPositionValues = new()
    {
        ['0'] = 0, ['1'] = 1, ['2'] = 2, ['3'] = 3, ['4'] = 4,
        ['5'] = 5, ['6'] = 6, ['7'] = 7, ['8'] = 8, ['9'] = 9,
        ['A'] = 0, ['B'] = 1, ['C'] = 2, ['D'] = 3, ['E'] = 4,
        ['F'] = 5, ['G'] = 6, ['H'] = 7, ['I'] = 8, ['J'] = 9,
        ['K'] = 10, ['L'] = 11, ['M'] = 12, ['N'] = 13, ['O'] = 14,
        ['P'] = 15, ['Q'] = 16, ['R'] = 17, ['S'] = 18, ['T'] = 19,
        ['U'] = 20, ['V'] = 21, ['W'] = 22, ['X'] = 23, ['Y'] = 24,
        ['Z'] = 25
    };

    /// <summary>
    /// Validates the checksum of an Italian Codice Fiscale for persona fisica (16 chars).
    /// The input must already be verified as a valid 16-character format before calling this method.
    /// </summary>
    /// <returns>True if the check character (position 16) matches the calculated checksum</returns>
    public static bool ValidateChecksum(string codiceFiscale)
    {
        if (string.IsNullOrEmpty(codiceFiscale) || codiceFiscale.Length != 16)
            return false;

        string cf = codiceFiscale.ToUpperInvariant();

        int sum = 0;

        // Process positions 1..15 (0-based index 0..14)
        for (int i = 0; i < 15; i++)
        {
            char c = cf[i];
            int position = i + 1; // 1-based position

            if (position % 2 == 1)
            {
                // Odd position (1, 3, 5, ..., 15)
                if (!OddPositionValues.TryGetValue(c, out int value))
                    return false;
                sum += value;
            }
            else
            {
                // Even position (2, 4, 6, ..., 14)
                if (!EvenPositionValues.TryGetValue(c, out int value))
                    return false;
                sum += value;
            }
        }

        // Modulo 26, convert to letter (0=A, 1=B, ..., 25=Z)
        int remainder = sum % 26;
        char expectedCheckChar = (char)('A' + remainder);

        return cf[15] == expectedCheckChar;
    }

    /// <summary>
    /// Gets a validation error message for the checksum, or empty string if valid.
    /// </summary>
    public static string GetValidationError(string codiceFiscale)
    {
        if (!ValidateChecksum(codiceFiscale))
            return "Codice Fiscale non valido (carattere di controllo errato)";

        return string.Empty;
    }
}
