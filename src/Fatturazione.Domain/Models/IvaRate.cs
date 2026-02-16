namespace Fatturazione.Domain.Models;

/// <summary>
/// Italian VAT (IVA - Imposta sul Valore Aggiunto) rates
/// </summary>
public enum IvaRate
{
    /// <summary>
    /// Standard rate (22%) - Aliquota ordinaria
    /// Applied to most goods and services
    /// </summary>
    Standard = 22,

    /// <summary>
    /// Reduced rate (10%) - Aliquota ridotta
    /// Applied to tourism, restaurants, some construction
    /// </summary>
    Reduced = 10,

    /// <summary>
    /// Intermediate rate (5%) - Aliquota intermedia
    /// Applied to social-health services, natural gas (first 480 mc/year)
    /// </summary>
    Intermediate = 5,

    /// <summary>
    /// Super-reduced rate (4%) - Aliquota super-ridotta
    /// Applied to essential goods (food, books, newspapers)
    /// </summary>
    SuperReduced = 4,

    /// <summary>
    /// Zero rate (0%) - Esente IVA
    /// Applied to exports, intra-EU transactions, specific services
    /// </summary>
    Zero = 0
}
