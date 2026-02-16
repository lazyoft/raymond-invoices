namespace Fatturazione.Domain.Models;

/// <summary>
/// Esigibilità IVA (Specifiche FatturaPA, campo 2.2.2.7)
/// </summary>
public enum EsigibilitaIva
{
    /// <summary>
    /// Esigibilità immediata (default)
    /// </summary>
    Immediata,

    /// <summary>
    /// Esigibilità differita
    /// </summary>
    Differita,

    /// <summary>
    /// Scissione dei pagamenti (Split Payment) - Art. 17-ter DPR 633/72
    /// </summary>
    SplitPayment
}
