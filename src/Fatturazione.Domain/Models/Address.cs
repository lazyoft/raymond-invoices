namespace Fatturazione.Domain.Models;

/// <summary>
/// Italian address information
/// </summary>
public class Address
{
    /// <summary>
    /// Street name and number (Via, Piazza, etc.)
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// City name (Comune)
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Province code (e.g., "MI" for Milano, "RM" for Roma)
    /// </summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>
    /// Postal code (CAP - Codice di Avviamento Postale)
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country (default: "Italia")
    /// </summary>
    public string Country { get; set; } = "Italia";
}
