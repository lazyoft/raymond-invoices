# Gap Analysis: Conformità Normativa Fatturazione Elettronica

**Data analisi:** 2026-02-16
**Scope:** Confronto tra normativa fiscale italiana e implementazione corrente
**Vincoli tecnici:** In-memory storage (no database)

---

## Legenda

| Simbolo | Significato |
|---------|-------------|
| IMPL | Implementato e conforme |
| PARTIAL | Parzialmente implementato — richiede completamento |
| MISSING | Non implementato — da sviluppare |
| N/A | Fuori scope (infrastruttura esterna, servizi terzi) |

---

## 1. Requisiti Obbligatori della Fattura (Art. 21 DPR 633/72)

### 1.1 Dati del Cedente/Prestatore (Emittente)

| Requisito | Stato | Note |
|-----------|-------|------|
| Denominazione / Ragione Sociale | MISSING | Non esiste un modello Emittente |
| Sede legale / Domicilio | MISSING | Nessun indirizzo emittente |
| Partita IVA emittente | MISSING | Solo il cliente è modellato |
| Regime fiscale emittente | MISSING | Es. RF01 (ordinario), RF19 (forfettario) |

**Cosa manca:** Il sistema modella solo il cessionario/committente (Client). Per legge, la fattura deve riportare anche i dati di chi la emette. Serve un modello `Issuer` (o `CedentePrestatore`) con: ragione sociale, indirizzo, P.IVA, codice fiscale, regime fiscale.

**Rif. normativo:** Art. 21, comma 2, lettere c) e d), DPR 633/72.

**Implementazione proposta:** Creare un modello `IssuerProfile` configurabile (singleton in-memory), referenziato da ogni fattura. Non serve un CRUD completo — basta un profilo emittente configurabile all'avvio.

---

### 1.2 Tipo Documento

| Requisito | Stato | Note |
|-----------|-------|------|
| Campo TipoDocumento (TD01, TD04, TD05, ...) | MISSING | Non distinguiamo tra fattura, nota di credito, parcella |
| Fattura immediata (TD01) | MISSING | Tutte le fatture sono "generiche" |
| Fattura differita (TD24) | MISSING | Nessun supporto |
| Parcella professionista (TD06) | MISSING | Non modellata |
| Fattura semplificata (TD07) | MISSING | Non modellata |

**Cosa manca:** Un enum `TipoDocumento` con almeno: TD01 (fattura), TD04 (nota credito), TD05 (nota debito), TD06 (parcella), TD24 (differita). Il tipo documento guida la logica di validazione e generazione XML.

**Rif. normativo:** Specifiche tecniche FatturaPA v1.2.2, campo 2.1.1.1.

**Implementazione proposta:** Aggiungere un enum `DocumentType` al modello `Invoice` con default TD01. Le note di credito/debito useranno lo stesso modello con tipo diverso.

---

### 1.3 Fattura Semplificata (Art. 21-bis DPR 633/72)

| Requisito | Stato | Note |
|-----------|-------|------|
| Supporto fattura semplificata (importo <= 400 EUR) | MISSING | - |
| Esenzione limite per forfettari (dal 01/01/2025) | MISSING | - |
| Requisiti ridotti (descrizione sintetica, solo CF/P.IVA) | MISSING | - |

**Cosa manca:** Per importi fino a 400 EUR (o senza limite per forfettari), il sistema dovrebbe permettere l'emissione di una fattura con requisiti semplificati. Attualmente tutte le fatture richiedono i dati completi.

**Rif. normativo:** Art. 21-bis DPR 633/72, DM 10/05/2019.

**Implementazione proposta:** Aggiungere un flag `IsSimplified` all'Invoice con validazione condizionale: se semplificata, rilassare i requisiti di dettaglio. Validare che importo <= 400 EUR (o forfettario senza limite).

---

## 2. IVA — Elementi Mancanti

### 2.1 Codici Natura IVA

| Requisito | Stato | Note |
|-----------|-------|------|
| Codici Natura (N1-N7) per operazioni a IVA 0% | MISSING | IvaRate.Zero non distingue il motivo dell'esenzione |
| N1 — Escluse ex art. 15 | MISSING | - |
| N2.1/N2.2 — Non soggette | MISSING | - |
| N3.1-N3.6 — Non imponibili | MISSING | - |
| N4 — Esenti (art. 10) | MISSING | - |
| N5 — Regime del margine | MISSING | - |
| N6.1-N6.9 — Inversione contabile | MISSING | - |
| N7 — IVA in altro stato UE | MISSING | - |

**Cosa manca:** Quando l'aliquota IVA è 0%, la normativa FatturaPA **obbliga** a specificare il codice Natura che giustifica l'assenza di IVA. Attualmente `IvaRate.Zero` è un valore generico senza qualificazione.

**Rif. normativo:** Specifiche tecniche FatturaPA, campo 2.2.1.14 (Natura).

**Implementazione proposta:** Creare un enum `NaturaIva` con tutti i codici N1-N7. Aggiungere un campo `NaturaIva?` a `InvoiceItem`. Validare: se `IvaRate == Zero`, allora `NaturaIva` è obbligatorio; se `IvaRate != Zero`, allora `NaturaIva` deve essere null.

---

### 2.2 Esigibilità IVA

| Requisito | Stato | Note |
|-----------|-------|------|
| Campo Esigibilità IVA (I/D/S) | MISSING | Non modellato |
| Immediata (I) — default | MISSING | - |
| Differita (D) — per specifici casi | MISSING | - |
| Split payment (S) — per PA | MISSING | Lo split payment è calcolato ma l'esigibilità non è esposta come campo |

**Cosa manca:** Il tracciato FatturaPA richiede il campo `EsigibilitaIVA` con valori I (immediata), D (differita), S (scissione pagamenti). Attualmente lo split payment è gestito nel calcolo ma non come metadato della fattura.

**Rif. normativo:** Specifiche FatturaPA, campo 2.2.2.7.

**Implementazione proposta:** Enum `EsigibilitaIva { Immediata, Differita, SplitPayment }`. Derivabile automaticamente: se `SubjectToSplitPayment` → S, altrimenti I (default).

---

### 2.3 Reverse Charge (Inversione Contabile)

| Requisito | Stato | Note |
|-----------|-------|------|
| Supporto operazioni in reverse charge | MISSING | Non modellato |
| Codici Natura N6.x associati | MISSING | Mancano i codici Natura |
| Validazione: IVA = 0 + Natura N6.x | MISSING | - |

**Cosa manca:** Per operazioni in reverse charge (subappalti edilizia, cessioni rottami, servizi internazionali, ecc.), l'IVA non viene esposta dal cedente ma assolta dal cessionario. Serve il supporto nel modello e nei calcoli.

**Rif. normativo:** Art. 17, commi 5-9, DPR 633/72.

**Implementazione proposta:** Gestibile tramite i codici Natura N6.x una volta implementati (punto 2.1). Non serve logica di calcolo aggiuntiva — basta validare che se Natura = N6.x, allora IvaRate = Zero.

---

## 3. Ritenuta d'Acconto — Elementi Mancanti

### 3.1 Tipologie di Ritenuta

| Requisito | Stato | Note |
|-----------|-------|------|
| Ritenuta professionisti 20% su 100% imponibile | IMPL | Funziona correttamente |
| Ritenuta agenti senza dipendenti 23% su 50% | MISSING | Solo una percentuale supportata |
| Ritenuta agenti con dipendenti 23% su 20% | MISSING | Solo una percentuale supportata |
| Ritenuta non residenti 30% su 100% | MISSING | Solo una percentuale supportata |
| Tipo ritenuta (RT01 persone fisiche, RT02 giuridiche) | MISSING | Non modellato |
| Causale pagamento (codice per CU) | MISSING | Non modellato |

**Cosa manca:** Il sistema supporta solo una percentuale piatta (default 20%). La normativa prevede basi di calcolo diverse (50%, 20% dell'imponibile per agenti) e aliquote diverse (23%, 30%). Serve un modello più articolato.

**Rif. normativo:** Art. 25 DPR 600/73, Art. 25-bis DPR 600/73.

**Implementazione proposta:** Estendere il modello con: `TipoRitenuta` (RT01/RT02), `AliquotaRitenuta`, `BaseCalcoloPercentuale` (100%, 50%, 20%). La formula diventa: `Ritenuta = Imponibile × BaseCalcolo% × Aliquota%`. Aggiungere `CausalePagamento` (codice A-Z per la CU).

---

## 4. Split Payment — Completamenti

| Requisito | Stato | Note |
|-----------|-------|------|
| Calcolo: TotalDue = Imponibile (IVA esclusa) | IMPL | Corretto |
| Mutua esclusione con ritenuta | IMPL | Validato nel calcolo |
| Annotazione "Scissione dei pagamenti — Art. 17-ter" | MISSING | Nessuna nota automatica |
| Esigibilità IVA = "S" | MISSING | Campo non presente (vedi 2.2) |

**Cosa manca:** La fattura con split payment dovrebbe riportare automaticamente la dicitura prevista dalla norma. Inoltre, il campo esigibilità IVA deve essere "S".

**Implementazione proposta:** Aggiungere una proprietà calcolata o una logica di generazione nota automatica quando `SubjectToSplitPayment = true`.

---

## 5. Imposta di Bollo — Completamenti

| Requisito | Stato | Note |
|-----------|-------|------|
| Bollo per regime forfettario > 77,47 EUR | IMPL | Funziona correttamente |
| Bollo per operazioni esenti IVA (Art. 10) > 77,47 EUR | MISSING | BolloService verifica solo `IsRegimeForfettario` |
| Bollo per operazioni fuori campo IVA > 77,47 EUR | MISSING | Non gestito |
| Bollo per operazioni non imponibili (escluse esportazioni) | MISSING | Non gestito |
| Fatture miste (parte imponibile, parte esente) | MISSING | Nessuna logica per porzione esente |
| Campo BolloVirtuale (SI/NO) per XML | MISSING | Non modellato |

**Cosa manca:** Il bollo attualmente si applica solo alle fatture forfettarie. In realtà va applicato a **tutte** le fatture in cui la parte non soggetta a IVA supera 77,47 EUR. Questo include: operazioni esenti (art. 10), fuori campo (artt. 2, 3, 5, 7), escluse base imponibile (art. 15).

**Rif. normativo:** Art. 13 DPR 642/72, Tabella allegata, Art. 6.

**Implementazione proposta:** Modificare `BolloService.RequiresBollo()` per verificare: (1) se forfettario E imponibile > 77,47; OPPURE (2) se la somma degli importi con `IvaRate.Zero` e Natura in {N1, N2.x, N3.5, N3.6, N4} supera 77,47 EUR. Richiede prima l'implementazione dei codici Natura (punto 2.1).

---

## 6. Note di Credito e Note di Debito (Art. 26 DPR 633/72)

| Requisito | Stato | Note |
|-----------|-------|------|
| Modello nota di credito (TD04) | MISSING | Non esiste |
| Modello nota di debito (TD05) | MISSING | Non esiste |
| Riferimento a fattura originaria | MISSING | Nessun campo di collegamento |
| Calcolo differenze importi | MISSING | - |
| Obbligo NC per annullamento fattura emessa | MISSING | Lo stato Cancelled non genera NC |
| NC semplificata (TD08) / ND semplificata (TD09) | MISSING | - |

**Cosa manca:** Questo è uno dei gap più rilevanti. Attualmente, annullare una fattura emessa (Issued, Sent, Overdue) la mette semplicemente in stato Cancelled senza creare la nota di credito richiesta dalla legge. Le note di credito e debito sono documenti fiscali autonomi con propria numerazione.

**Rif. normativo:** Art. 26 DPR 633/72.

**Implementazione proposta:** Riutilizzare il modello `Invoice` con `DocumentType = TD04/TD05`. Aggiungere:
- `RelatedInvoiceId: Guid?` — riferimento alla fattura originaria
- `RelatedInvoiceNumber: string?` — numero fattura di riferimento
- Validazione: una NC/ND deve sempre riferirsi a una fattura emessa
- Numerazione separata o nella stessa serie (scelta dell'utente)
- Endpoint: `POST /api/invoices/{id}/credit-note` per generare NC da fattura esistente
- Modifica transizione Cancelled: se la fattura è emessa (non Draft), richiedere la creazione di una NC

---

## 7. Ciclo di Vita — Completamenti

### 7.1 Transizioni di Stato Mancanti

| Requisito | Stato | Note |
|-----------|-------|------|
| Draft → Issued (con assegnazione numero) | IMPL | Endpoint `/issue` |
| Draft → Cancelled | IMPL | Validazione presente |
| Issued → Sent | MISSING | Nessun endpoint per questa transizione |
| Issued → Cancelled (con obbligo NC) | PARTIAL | Transizione validata ma NC non generata |
| Sent → Paid | MISSING | Nessun endpoint |
| Sent → Overdue | MISSING | Nessun endpoint |
| Sent → Cancelled (con obbligo NC) | PARTIAL | Transizione validata ma NC non generata |
| Overdue → Paid | MISSING | Nessun endpoint |
| Overdue → Cancelled (con obbligo NC) | PARTIAL | Transizione validata ma NC non generata |

**Cosa manca:** La logica di validazione delle transizioni è implementata in `InvoiceValidator.CanTransitionTo()`, ma manca un endpoint generico per eseguire le transizioni. Attualmente esiste solo `/issue` (Draft → Issued).

**Implementazione proposta:** Aggiungere un endpoint `POST /api/invoices/{id}/transition` che accetta lo stato di destinazione. La logica di validazione esiste già — serve solo l'endpoint che la invoca.

---

### 7.2 Immutabilità Post-Emissione

| Requisito | Stato | Note |
|-----------|-------|------|
| PUT bloccato su fatture non-Draft | IMPL | Verificato e testato |
| Ricalcolo all'emissione | IMPL | L'endpoint `/issue` ricalcola i totali |

---

## 8. Condizioni e Modalità di Pagamento

| Requisito | Stato | Note |
|-----------|-------|------|
| Modalità di pagamento (MP01-MP23) | MISSING | Nessun campo |
| IBAN beneficiario | MISSING | Nessun campo |
| Condizioni pagamento (TP01 rate, TP02 completo, TP03 anticipo) | MISSING | Nessun campo |
| Termini di pagamento (30gg DF, 60gg FM, ecc.) | MISSING | Solo DueDate come campo data |
| Sconto per pagamento anticipato | MISSING | - |

**Cosa manca:** Il blocco DatiPagamento (2.4) della FatturaPA è completamente assente. Il sistema ha solo `DueDate` ma nessuna informazione su come e dove pagare.

**Rif. normativo:** Specifiche FatturaPA, blocco 2.4.

**Implementazione proposta:** Creare un modello `PaymentInfo`:
- `ModalitaPagamento: PaymentMethod` (enum: Bonifico, RiBa, Contanti, CartaCredito, ...)
- `IBAN: string?`
- `CondizioniPagamento: PaymentCondition` (enum: Completo, Rate, Anticipo)
- `BancaAppoggio: string?`
Aggiungere come proprietà opzionale dell'Invoice. Per l'emittente forfettario/professionista, l'IBAN è quasi sempre necessario.

---

## 9. Fatturazione Elettronica (FatturaPA XML)

| Requisito | Stato | Note |
|-----------|-------|------|
| Generazione XML formato FatturaPA | MISSING | Nessun export |
| Validazione XML contro schema XSD | MISSING | - |
| Codice Destinatario (7 caratteri) | MISSING | Non modellato su Client |
| PEC destinatario | MISSING | Non modellato su Client |
| Trasmissione a SDI | N/A | Richiede servizio esterno / intermediario |
| Ricezione esiti da SDI | N/A | Richiede servizio esterno |
| Conservazione digitale a norma | N/A | Richiede servizio certificato |
| Firma digitale (XAdES/CAdES) | N/A | Richiede certificato di firma |

**Cosa manca lato modello dati:**
- `CodiceDestinatario: string?` sul Client (7 caratteri per B2B, 6 per PA — attualmente `CodiceUnivocoUfficio` copre solo PA)
- `PEC: string?` sul Client (alternativa al codice destinatario)

**Cosa manca lato funzionalità:**
- Un servizio di generazione XML FatturaPA che mappi il modello interno nel tracciato XML standard
- Un endpoint `GET /api/invoices/{id}/xml` per scaricare la fattura in formato FatturaPA

**Rif. normativo:** DL 119/2018, DL 127/2015, Provvedimento AdE 89757/2018.

**Implementazione proposta (fase 1 — modello dati):** Aggiungere `CodiceDestinatario` e `PEC` al modello Client. Per PA, il `CodiceUnivocoUfficio` esistente funge da Codice Destinatario.

**Implementazione proposta (fase 2 — generazione XML):** Creare un servizio `FatturaPAXmlGenerator` che produca l'XML conforme. Questa è una feature complessa ma autocontenuta — può essere implementata dopo che tutti i campi di modello sono presenti.

---

## 10. Validazioni Mancanti o Incomplete

### 10.1 Validazioni Client

| Campo | Stato | Gap |
|-------|-------|-----|
| RagioneSociale non vuota | IMPL | - |
| Email formato valido | IMPL | - |
| Partita IVA checksum | IMPL | - |
| Codice Fiscale formato | IMPL | Solo formato, non checksum lettera di controllo |
| CodiceUnivocoUfficio per PA | IMPL | - |
| CIG formato (10 alfanumerici) | PARTIAL | Campo presente ma formato non validato |
| CUP formato (15 alfanumerici) | PARTIAL | Campo presente ma formato non validato |
| PostalCode 5 cifre | IMPL | - |
| Province 2 lettere | IMPL | - |
| CodiceDestinatario (7 caratteri B2B) | MISSING | Campo non presente |

### 10.2 Validazioni Invoice

| Regola | Stato | Gap |
|--------|-------|-----|
| ClientId obbligatorio | IMPL | - |
| Date coerenti (DueDate >= InvoiceDate) | IMPL | - |
| Almeno un item | IMPL | - |
| Quantity > 0 | IMPL | - |
| UnitPrice >= 0 | IMPL | - |
| Data fattura non nel futuro | MISSING | Nessuna validazione |
| NaturaIva obbligatoria se IvaRate = Zero | MISSING | NaturaIva non esiste ancora |
| Causale obbligatoria per forfettario | MISSING | Nessun campo dedicato né validazione |
| Coerenza split payment / tipo cliente | MISSING | Non si verifica che split payment sia solo per PA |
| Coerenza ritenuta / regime forfettario | PARTIAL | Il calcolo azzera la ritenuta ma non c'è validazione esplicita |

### 10.3 Validazioni Codice Fiscale

| Requisito | Stato | Note |
|-----------|-------|------|
| Formato 16 alfanumerici (persona fisica) | IMPL | Solo controllo lunghezza e formato |
| Formato 11 cifre (persona giuridica) | IMPL | Solo controllo lunghezza e formato |
| Algoritmo checksum lettera di controllo (persona fisica) | MISSING | Non implementato |
| Validazione coerenza con dati anagrafici | N/A | Richiederebbe tabelle codici catastali |

---

## 11. Regime Forfettario — Completamenti

| Requisito | Stato | Note |
|-----------|-------|------|
| IVA = 0 su tutte le righe | IMPL | Forzato nel calcolo |
| Ritenuta = 0 | IMPL | Forzato nel calcolo |
| Bollo se > 77,47 EUR | IMPL | Calcolato |
| Dicitura obbligatoria in fattura | MISSING | Nessuna validazione/generazione automatica |
| Natura IVA = N2.2 per le righe | MISSING | NaturaIva non implementata |
| Limite ricavi 85.000 EUR annui | MISSING | Nessun controllo (informativo, non bloccante) |

**Cosa manca:** La legge richiede che le fatture in regime forfettario contengano la dicitura *"Operazione effettuata ai sensi dell'art. 1, commi 54-89, Legge n. 190/2014"*. Il sistema dovrebbe validare la presenza di questa nota o generarla automaticamente.

**Implementazione proposta:** Aggiungere un campo `Causale: string?` all'Invoice. Validare che se `IsRegimeForfettario = true`, allora `Causale` deve contenere il riferimento normativo. In alternativa, generarla automaticamente all'emissione.

---

## 12. Sconti — Completamenti

| Requisito | Stato | Note |
|-----------|-------|------|
| Sconto percentuale a livello riga | IMPL | `DiscountPercentage` su InvoiceItem |
| Sconto fisso a livello riga | IMPL | `DiscountAmount` su InvoiceItem |
| Sconto/maggiorazione a livello documento | MISSING | Non modellato |
| Distinzione sconto condizionato/incondizionato | MISSING | Nessun flag |

**Implementazione proposta:** Aggiungere a `Invoice`:
- `DocumentDiscountPercentage: decimal`
- `DocumentDiscountAmount: decimal`
Gli sconti a livello documento riducono la base imponibile complessiva dopo il totale delle righe.

---

## 13. Termini di Emissione

| Requisito | Stato | Note |
|-----------|-------|------|
| Fattura immediata: emissione entro 12 gg dall'operazione | MISSING | Nessun controllo |
| Fattura differita (TD24): entro 15 del mese successivo | MISSING | Tipo documento non gestito |
| Data operazione vs data fattura | MISSING | Nessun campo "data operazione" |

**Implementazione proposta:** Aggiungere un campo opzionale `DataOperazione: DateTime?` all'Invoice. Se presente, validare che la data fattura sia entro 12 giorni dalla data operazione (per fatture immediate). Per fatture differite (TD24), validare che sia entro il 15 del mese successivo.

---

## Riepilogo Priorità

### Priorità Alta (conformità legale di base)

| # | Gap | Impatto |
|---|-----|---------|
| 1 | **Dati emittente** (Sez. 1.1) | Senza dati emittente la fattura è incompleta per legge |
| 2 | **Note di credito/debito** (Sez. 6) | Impossibile annullare correttamente una fattura emessa |
| 3 | **Codici Natura IVA** (Sez. 2.1) | Obbligatori per operazioni a IVA 0% in fattura elettronica |
| 4 | **Tipo Documento** (Sez. 1.2) | Necessario per distinguere fatture da note credito/debito |
| 5 | **Endpoint transizioni di stato** (Sez. 7.1) | Solo Draft→Issued ha un endpoint; le altre transizioni non sono eseguibili |
| 6 | **Causale obbligatoria forfettario** (Sez. 11) | Requisito di legge per fatture in regime forfettario |

### Priorità Media (completezza funzionale)

| # | Gap | Impatto |
|---|-----|---------|
| 7 | **Condizioni di pagamento** (Sez. 8) | Blocco obbligatorio nel tracciato FatturaPA |
| 8 | **Bollo per tutte le operazioni esenti** (Sez. 5) | Attualmente limitato al solo regime forfettario |
| 9 | **Esigibilità IVA** (Sez. 2.2) | Campo obbligatorio nel tracciato FatturaPA |
| 10 | **Codice Destinatario / PEC** (Sez. 9) | Necessario per la trasmissione elettronica |
| 11 | **Tipologie ritenuta avanzate** (Sez. 3.1) | Necessario per agenti e non residenti |
| 12 | **Validazione CIG/CUP** (Sez. 10.1) | Campi presenti ma non validati nel formato |

### Priorità Bassa (estensioni e ottimizzazioni)

| # | Gap | Impatto |
|---|-----|---------|
| 13 | **Generazione XML FatturaPA** (Sez. 9) | Feature complessa, richiede tutti i campi modello prima |
| 14 | **Fattura semplificata** (Sez. 1.3) | Caso d'uso limitato (< 400 EUR) |
| 15 | **Sconti a livello documento** (Sez. 12) | Nice to have |
| 16 | **Checksum Codice Fiscale** (Sez. 10.3) | Formato validato, manca solo il checksum |
| 17 | **Termini di emissione** (Sez. 13) | Validazione temporale, non bloccante |
| 18 | **Reverse charge** (Sez. 2.3) | Gestibile via codici Natura una volta implementati |

### Fuori Scope (servizi esterni)

| # | Gap | Motivo |
|---|-----|--------|
| 19 | Trasmissione a SDI | Richiede intermediario accreditato |
| 20 | Conservazione digitale a norma | Richiede servizio certificato |
| 21 | Firma digitale XML | Richiede certificato di firma |
| 22 | Ricezione esiti SDI | Richiede integrazione asincrona |

---

## Stato dei Bug Documentati (Sezione 14 del documento di dominio)

| # | Bug/Gap | Stato |
|---|---------|-------|
| 1 | Ritenuta calcolata su SubTotal invece di ImponibileTotal | RISOLTO |
| 2 | Numerazione non resetta al cambio anno | RISOLTO |
| 3 | Dati dell'emittente mancanti | DA FARE (Sez. 1.1) |
| 4 | Aliquota IVA 5% mancante | RISOLTO (IvaRate.Intermediate) |
| 5 | Codici Natura IVA mancanti | DA FARE (Sez. 2.1) |
| 6 | Split payment per PA | RISOLTO |
| 7 | Regime forfettario | RISOLTO |
| 8 | Note di credito/debito | DA FARE (Sez. 6) |
| 9 | Fatturazione elettronica XML/SDI | DA FARE (Sez. 9) |
| 10 | CUU, CIG, CUP per PA | RISOLTO (campi e validazione CUU presenti) |
| 11 | Imposta di bollo | PARZIALE (solo forfettario, non tutte le operazioni esenti) |
| 12 | Condizioni e modalità di pagamento | DA FARE (Sez. 8) |
| 13 | Reverse charge | DA FARE (Sez. 2.3) |
| 14 | Immutabilità fattura emessa | RISOLTO |
| 15 | Validazione Codice Fiscale | PARZIALE (formato sì, checksum no) |
| 16 | Validazione campi obbligatori incompleta | RISOLTO (Quantity, UnitPrice, RagioneSociale validati) |
| 17 | Conservazione digitale a norma | FUORI SCOPE |
