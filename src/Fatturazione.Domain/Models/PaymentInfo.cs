namespace Fatturazione.Domain.Models;

/// <summary>
/// Informazioni di pagamento (Specifiche FatturaPA, blocco 2.4)
/// </summary>
public class PaymentInfo
{
    /// <summary>
    /// Condizioni di pagamento (rate, completo, anticipo)
    /// </summary>
    public PaymentCondition Condizioni { get; set; } = PaymentCondition.TP02_Completo;

    /// <summary>
    /// Modalità di pagamento
    /// </summary>
    public PaymentMethod Modalita { get; set; } = PaymentMethod.MP05_Bonifico;

    /// <summary>
    /// IBAN del beneficiario
    /// </summary>
    public string? IBAN { get; set; }

    /// <summary>
    /// Nome della banca di appoggio
    /// </summary>
    public string? BancaAppoggio { get; set; }
}
