namespace Fatturazione.Domain.Models;

/// <summary>
/// Modalità di pagamento (Specifiche FatturaPA, campo 2.4.2.2)
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// MP01 - Contanti
    /// </summary>
    MP01_Contanti,

    /// <summary>
    /// MP02 - Assegno
    /// </summary>
    MP02_Assegno,

    /// <summary>
    /// MP03 - Assegno circolare
    /// </summary>
    MP03_AssegnoCircolare,

    /// <summary>
    /// MP04 - Contanti presso Tesoreria
    /// </summary>
    MP04_ContantiTesoreria,

    /// <summary>
    /// MP05 - Bonifico
    /// </summary>
    MP05_Bonifico,

    /// <summary>
    /// MP06 - Vaglia cambiario
    /// </summary>
    MP06_VagliaCambiario,

    /// <summary>
    /// MP07 - Bollettino bancario
    /// </summary>
    MP07_BollettinoBancario,

    /// <summary>
    /// MP08 - Carta di pagamento
    /// </summary>
    MP08_CartaDiPagamento,

    /// <summary>
    /// MP09 - RID
    /// </summary>
    MP09_RID,

    /// <summary>
    /// MP10 - RID utenze
    /// </summary>
    MP10_RIDUtenze,

    /// <summary>
    /// MP11 - RID veloce
    /// </summary>
    MP11_RIDVeloce,

    /// <summary>
    /// MP12 - RIBA (Ricevuta Bancaria)
    /// </summary>
    MP12_RIBA,

    /// <summary>
    /// MP13 - MAV (Mediante Avviso)
    /// </summary>
    MP13_MAV,

    /// <summary>
    /// MP14 - Quietanza erario
    /// </summary>
    MP14_QuietanzaErario,

    /// <summary>
    /// MP15 - Giroconto su conti di contabilità speciale
    /// </summary>
    MP15_Giroconto,

    /// <summary>
    /// MP16 - Domiciliazione bancaria
    /// </summary>
    MP16_DomiciliazioneBancaria,

    /// <summary>
    /// MP17 - Domiciliazione postale
    /// </summary>
    MP17_DomiciliazionePostale,

    /// <summary>
    /// MP18 - Bollettino di c/c postale
    /// </summary>
    MP18_BollettinoPostale,

    /// <summary>
    /// MP19 - SEPA Direct Debit
    /// </summary>
    MP19_SEPA,

    /// <summary>
    /// MP20 - SEPA Direct Debit CORE
    /// </summary>
    MP20_SEPA_CORE,

    /// <summary>
    /// MP21 - SEPA Direct Debit B2B
    /// </summary>
    MP21_SEPA_B2B,

    /// <summary>
    /// MP22 - Trattenuta su somme già riscosse
    /// </summary>
    MP22_Trattenuta,

    /// <summary>
    /// MP23 - PagoPA
    /// </summary>
    MP23_PagoPA
}
