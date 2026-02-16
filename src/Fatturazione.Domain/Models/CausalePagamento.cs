namespace Fatturazione.Domain.Models;

/// <summary>
/// Causale pagamento per la Certificazione Unica (Specifiche FatturaPA, campo 2.1.1.5.4)
/// </summary>
public enum CausalePagamento
{
    /// <summary>
    /// A - Prestazioni di lavoro autonomo rientranti nell'esercizio di arte o professione abituale
    /// </summary>
    A,

    /// <summary>
    /// B - Utilizzazione economica di opere dell'ingegno, brevetti industriali e simili
    /// </summary>
    B,

    /// <summary>
    /// C - Utili derivanti da contratti di associazione in partecipazione
    /// </summary>
    C,

    /// <summary>
    /// D - Utili spettanti ai soci promotori e ai soci fondatori di società di capitali
    /// </summary>
    D,

    /// <summary>
    /// E - Levata di protesti cambiari da parte dei segretari comunali
    /// </summary>
    E,

    /// <summary>
    /// G - Indennità corrisposte per la cessazione di attività sportiva professionale
    /// </summary>
    G,

    /// <summary>
    /// H - Indennità corrisposte per la cessazione dei rapporti di agenzia
    /// </summary>
    H,

    /// <summary>
    /// I - Indennità corrisposte per la cessazione da funzioni notarili
    /// </summary>
    I,

    /// <summary>
    /// L - Utilizzazione economica di opere dell'ingegno di persone non residenti
    /// </summary>
    L,

    /// <summary>
    /// M - Prestazioni di lavoro autonomo non esercitate abitualmente
    /// </summary>
    M,

    /// <summary>
    /// N - Indennità di trasferta, rimborsi forfettari di spesa, premi e compensi erogati nell'ambito sportivo dilettantistico
    /// </summary>
    N,

    /// <summary>
    /// O - Prestazioni di lavoro autonomo non esercitate abitualmente (no obblighi previdenziali)
    /// </summary>
    O,

    /// <summary>
    /// P - Compensi corrisposti a soggetti non residenti privi di stabile organizzazione
    /// </summary>
    P,

    /// <summary>
    /// Q - Provvigioni corrisposte ad agente o rappresentante di commercio monomandatario
    /// </summary>
    Q,

    /// <summary>
    /// R - Provvigioni corrisposte ad agente o rappresentante di commercio plurimandatario
    /// </summary>
    R,

    /// <summary>
    /// S - Provvigioni corrisposte a commissionario
    /// </summary>
    S,

    /// <summary>
    /// T - Provvigioni corrisposte a mediatore
    /// </summary>
    T,

    /// <summary>
    /// U - Provvigioni corrisposte a procacciatore di affari
    /// </summary>
    U,

    /// <summary>
    /// V - Provvigioni corrisposte a incaricato per le vendite a domicilio
    /// </summary>
    V,

    /// <summary>
    /// W - Corrispettivi erogati nel 2013 per prestazioni relative a contratti d'appalto
    /// </summary>
    W,

    /// <summary>
    /// X - Canoni corrisposti nel 2004 da società o enti residenti
    /// </summary>
    X,

    /// <summary>
    /// Y - Canoni corrisposti dal 2005 da società o enti residenti
    /// </summary>
    Y,

    /// <summary>
    /// Z - Titolo diverso dai precedenti
    /// </summary>
    Z,

    /// <summary>
    /// ZO - Titolo diverso dai precedenti (ulteriore specifica)
    /// </summary>
    ZO
}
