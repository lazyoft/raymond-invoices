namespace Fatturazione.Domain.Models;

/// <summary>
/// Tipo ritenuta d'acconto (Specifiche FatturaPA, campo 2.1.1.5.1)
/// </summary>
public enum TipoRitenuta
{
    /// <summary>
    /// Ritenuta persone fisiche
    /// </summary>
    RT01,

    /// <summary>
    /// Ritenuta persone giuridiche
    /// </summary>
    RT02
}
