namespace Fatturazione.Domain.Models;

/// <summary>
/// Codici Natura IVA per operazioni a aliquota 0% (Specifiche FatturaPA, campo 2.2.1.14)
/// Obbligatorio quando IvaRate == Zero
/// </summary>
public enum NaturaIva
{
    /// <summary>
    /// Escluse ex art. 15 DPR 633/72
    /// </summary>
    N1,

    /// <summary>
    /// Non soggette - artt. da 7 a 7-septies
    /// </summary>
    N2_1,

    /// <summary>
    /// Non soggette - altri casi (include regime forfettario)
    /// </summary>
    N2_2,

    /// <summary>
    /// Non imponibili - esportazioni
    /// </summary>
    N3_1,

    /// <summary>
    /// Non imponibili - cessioni intracomunitarie
    /// </summary>
    N3_2,

    /// <summary>
    /// Non imponibili - cessioni verso San Marino
    /// </summary>
    N3_3,

    /// <summary>
    /// Non imponibili - assimilate a esportazioni
    /// </summary>
    N3_4,

    /// <summary>
    /// Non imponibili - dichiarazioni d'intento
    /// </summary>
    N3_5,

    /// <summary>
    /// Non imponibili - altre operazioni
    /// </summary>
    N3_6,

    /// <summary>
    /// Esenti (art. 10 DPR 633/72)
    /// </summary>
    N4,

    /// <summary>
    /// Regime del margine / IVA non esposta
    /// </summary>
    N5,

    /// <summary>
    /// Inversione contabile - cessione rottami
    /// </summary>
    N6_1,

    /// <summary>
    /// Inversione contabile - cessione oro/argento
    /// </summary>
    N6_2,

    /// <summary>
    /// Inversione contabile - subappalto edilizia
    /// </summary>
    N6_3,

    /// <summary>
    /// Inversione contabile - cessione fabbricati
    /// </summary>
    N6_4,

    /// <summary>
    /// Inversione contabile - cessione telefoni cellulari
    /// </summary>
    N6_5,

    /// <summary>
    /// Inversione contabile - cessione prodotti elettronici
    /// </summary>
    N6_6,

    /// <summary>
    /// Inversione contabile - prestazioni comparto edile
    /// </summary>
    N6_7,

    /// <summary>
    /// Inversione contabile - operazioni settore energetico
    /// </summary>
    N6_8,

    /// <summary>
    /// Inversione contabile - altri casi
    /// </summary>
    N6_9,

    /// <summary>
    /// IVA assolta in altro stato UE
    /// </summary>
    N7
}
