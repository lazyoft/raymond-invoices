namespace Fatturazione.Domain.Models;

/// <summary>
/// Tipo Documento per fattura elettronica (Specifiche FatturaPA, campo 2.1.1.1)
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Fattura immediata
    /// </summary>
    TD01,

    /// <summary>
    /// Acconto/anticipo su fattura
    /// </summary>
    TD02,

    /// <summary>
    /// Nota di credito (Art. 26 DPR 633/72)
    /// </summary>
    TD04,

    /// <summary>
    /// Nota di debito (Art. 26 DPR 633/72)
    /// </summary>
    TD05,

    /// <summary>
    /// Parcella professionista
    /// </summary>
    TD06,

    /// <summary>
    /// Fattura semplificata (Art. 21-bis DPR 633/72)
    /// </summary>
    TD07,

    /// <summary>
    /// Nota di credito semplificata
    /// </summary>
    TD08,

    /// <summary>
    /// Nota di debito semplificata
    /// </summary>
    TD09,

    /// <summary>
    /// Fattura differita (Art. 21, comma 4, lett. a)
    /// </summary>
    TD24
}
