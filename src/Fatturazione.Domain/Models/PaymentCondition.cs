namespace Fatturazione.Domain.Models;

/// <summary>
/// Condizioni di pagamento (Specifiche FatturaPA, campo 2.4.1)
/// </summary>
public enum PaymentCondition
{
    /// <summary>
    /// TP01 - Pagamento a rate
    /// </summary>
    TP01_Rate,

    /// <summary>
    /// TP02 - Pagamento completo
    /// </summary>
    TP02_Completo,

    /// <summary>
    /// TP03 - Anticipo
    /// </summary>
    TP03_Anticipo
}
