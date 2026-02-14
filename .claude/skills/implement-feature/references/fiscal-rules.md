# Dominio di Business: Fatturazione Elettronica Italiana

Questo documento descrive le regole fiscali e normative italiane applicabili al sistema di fatturazione. Va usato come riferimento per validare i requisiti di ogni elemento dell'applicazione.

**Riferimenti normativi principali:**

- DPR 633/1972 (Testo Unico IVA)
- DPR 600/1973 (Ritenute d'acconto)
- DPR 642/1972 (Imposta di bollo)
- DL 119/2018 e DL 127/2015 (Fatturazione elettronica)
- Legge 190/2014 (Regime forfettario e split payment)

---

## 1. La Fattura: Requisiti Obbligatori per Legge

### 1.1 Fattura Ordinaria (Art. 21 DPR 633/72)

Ogni fattura **deve** contenere i seguenti elementi:

**Dati identificativi del documento:**

- Data di emissione
- Numero progressivo che identifichi la fattura in modo univoco

**Dati del cedente/prestatore (chi emette la fattura):**

- Denominazione o ragione sociale (oppure nome e cognome per persone fisiche)
- Residenza o domicilio / sede legale
- Numero di Partita IVA

**Dati del cessionario/committente (il cliente):**

- Denominazione o ragione sociale (oppure nome e cognome)
- Residenza o domicilio / sede legale
- Numero di Partita IVA (o Codice Fiscale per consumatori finali)

**Dati dell'operazione:**

- Natura, qualita e quantita dei beni ceduti o dei servizi prestati
- Corrispettivi (prezzo unitario) e dati necessari per la determinazione della base imponibile (sconti, abbuoni, premi)
- Aliquota IVA applicata
- Ammontare dell'imposta (IVA) e dell'imponibile, con arrotondamento al centesimo di euro

> **Implicazione per l'applicazione:** Il sistema attualmente non gestisce i dati dell'emittente (cedente/prestatore). In una fattura reale, questi dati sono obbligatori tanto quanto quelli del cliente.

### 1.2 Fattura Semplificata (Art. 21-bis DPR 633/72)

Puo essere emessa quando l'importo complessivo non supera **400 euro** (soglia dal DM 10/05/2019).

**Eccezione dal 1 gennaio 2025:** i contribuenti in regime forfettario possono emettere fattura semplificata **senza limite di importo**.

Differenze rispetto alla fattura ordinaria:

- L'operazione puo essere descritta in modo sintetico
- Non e richiesta la distinta esposizione di imponibile e imposta
- Per il cliente e sufficiente il Codice Fiscale o la Partita IVA (non tutti i dati anagrafici)

---

## 2. Soggetti Fiscali: Tipologie e Identificativi

### 2.1 Partita IVA

**Struttura:** esattamente 11 cifre numeriche.

| Posizione | Significato |
|-----------|-------------|
| Cifre 1-7 | Numero progressivo (matricola) del contribuente |
| Cifre 8-10 | Codice dell'Ufficio Provinciale che l'ha attribuita |
| Cifra 11 | Carattere di controllo (checksum) |

**Algoritmo di validazione:** La cifra di controllo si calcola con una variante dell'algoritmo di Luhn:

- Le cifre in posizione dispari (1a, 3a, 5a, 7a, 9a) si sommano direttamente
- Le cifre in posizione pari (2a, 4a, 6a, 8a, 10a) si raddoppiano; se il risultato supera 9, si sottrae 9
- Si sommano tutti i risultati
- La cifra di controllo = (10 - (somma mod 10)) mod 10

**Formato europeo:** prefisso "IT" seguito dalle 11 cifre (es. IT01234567890).

### 2.2 Codice Fiscale

**Per persone fisiche:** 16 caratteri alfanumerici:

- 3 caratteri dal cognome
- 3 caratteri dal nome
- 2 cifre per l'anno di nascita
- 1 lettera per il mese di nascita
- 2 cifre per il giorno di nascita (per le donne si aggiunge 40)
- 4 caratteri per il codice catastale del comune di nascita
- 1 carattere di controllo

**Per persone giuridiche:** 11 cifre numeriche, stessa struttura della Partita IVA. Spesso coincide con la P.IVA stessa.

### 2.3 Tipologie di Clienti

| Tipo | Caratteristiche Fiscali |
|------|------------------------|
| **Professionista** | Soggetto a ritenuta d'acconto. Esercita lavoro autonomo abituale (medici, avvocati, ingegneri, consulenti). La ritenuta viene trattenuta dal committente e versata all'Erario. |
| **Azienda** (SRL, SPA, SRLS) | NON soggetta a ritenuta d'acconto. L'IVA si applica normalmente. |
| **Pubblica Amministrazione** | Soggetta al meccanismo di **split payment**: l'IVA esposta in fattura non viene incassata dal fornitore, ma versata direttamente dalla PA all'Erario. Richiede Codice Univoco Ufficio (IPA), CIG e CUP. |

**Caso speciale - Regime Forfettario** (Legge 190/2014, commi 54-89):

- Soglia ricavi/compensi: massimo 85.000 euro annui
- Le operazioni NON sono soggette ad IVA (non si addebita e non si detrae)
- I compensi NON sono soggetti a ritenuta d'acconto
- In fattura va indicato: "Operazione effettuata ai sensi dell'art. 1, commi 54-89, Legge n. 190/2014"
- Imposta sostitutiva: 15% (o 5% per i primi 5 anni di start-up)

---

## 3. IVA (Imposta sul Valore Aggiunto)

### 3.1 Aliquote IVA Vigenti

| Aliquota | Nome | Applicazione | Riferimento |
|----------|------|-------------|-------------|
| **22%** | Ordinaria | Tutti i beni e servizi non espressamente agevolati | Art. 16 DPR 633/72 |
| **10%** | Ridotta | Carni, pesce, riso, uova, medicinali, turismo, ristorazione, ristrutturazioni edilizie, energia domestica | Tabella A, Parte III |
| **5%** | Intermedia | Prestazioni socio-sanitarie di cooperative sociali, alcune erbe aromatiche fresche, gas metano civile (primi 480 mc/anno) | Tabella A, Parte II-bis |
| **4%** | Super-ridotta | Generi alimentari di prima necessita (pane, latte, frutta, verdura), prima casa, ausili per disabili, editoria | Tabella A, Parte II |
| **0%** | Esente / Non imponibile | Vedi sotto | Artt. 8, 9, 10 DPR 633/72 |

> **Stato attuale nell'applicazione:** L'aliquota del 5% non e gestita. Il sistema ha solo: Standard (22%), Reduced (10%), SuperReduced (4%), Zero (0%). Per un sistema completo, l'aliquota del 5% va aggiunta.

### 3.2 Operazioni Non Imponibili

Non generano IVA ma concedono il diritto alla detrazione IVA sugli acquisti:

- **Art. 8 DPR 633/72** - Cessioni all'esportazione (vendite verso paesi extra-UE)
- **Art. 8-bis** - Operazioni assimilate (costruzione/riparazione navi, aeromobili)
- **Art. 9** - Servizi internazionali (trasporti internazionali, operazioni portuali e aeroportuali)

### 3.3 Operazioni Esenti (Art. 10 DPR 633/72)

Non generano IVA e NON concedono il diritto alla detrazione. Le principali:

- Prestazioni sanitarie di diagnosi, cura e riabilitazione
- Prestazioni educative e didattiche
- Operazioni di credito e finanziamento
- Operazioni assicurative
- Locazioni di immobili (con eccezioni)

### 3.4 Esclusioni dalla Base Imponibile (Art. 15 DPR 633/72)

Non concorrono a formare la base imponibile IVA:

- Interessi moratori per ritardo nel pagamento
- Beni ceduti a titolo di sconto, abbuono o premio
- Spese anticipate in nome e per conto del cliente (documentate)
- Imballaggi e recipienti a cauzione

### 3.5 Codici Natura IVA nella Fattura Elettronica

Quando l'aliquota e 0%, nella fattura elettronica e **obbligatorio** specificare il codice natura:

| Codice | Significato |
|--------|------------|
| N1 | Escluse ex art. 15 |
| N2.1 | Non soggette - artt. da 7 a 7-septies |
| N2.2 | Non soggette - altri casi |
| N3.1 | Non imponibili - esportazioni |
| N3.2 | Non imponibili - cessioni intracomunitarie |
| N3.3 | Non imponibili - cessioni verso San Marino |
| N3.4 | Non imponibili - assimilate a esportazioni |
| N3.5 | Non imponibili - dichiarazioni d'intento |
| N3.6 | Non imponibili - altre operazioni |
| N4 | Esenti |
| N5 | Regime del margine / IVA non esposta |
| N6.1 | Inversione contabile - cessione rottami |
| N6.2 | Inversione contabile - cessione oro/argento |
| N6.3 | Inversione contabile - subappalto edilizia |
| N6.4-N6.9 | Inversione contabile - altri casi specifici |
| N7 | IVA assolta in altro stato UE |

### 3.6 Reverse Charge (Art. 17 DPR 633/72)

Meccanismo di inversione contabile in cui l'IVA viene assolta dal cessionario/committente anziche dal cedente/prestatore. Si applica in:

- Edilizia (subappalti, servizi di pulizia, demolizione, installazione impianti)
- Cessioni di prodotti elettronici (fase pre-dettaglio)
- Cessioni di oro, argento, rottami
- Settore energetico (gas, energia elettrica)
- Operazioni con soggetti non stabiliti in Italia

---

## 4. Ritenuta d'Acconto

### 4.1 Quando si Applica (Art. 25 DPR 600/73)

La ritenuta d'acconto si applica quando un **sostituto d'imposta** (chi paga) corrisponde compensi a:

- Lavoratori autonomi / professionisti
- Agenti e rappresentanti di commercio
- Collaboratori con prestazione occasionale
- Intermediari e mediatori

**Il sostituto d'imposta** e il soggetto che paga il compenso. E obbligato a:

1. Trattenere la ritenuta dal compenso
2. Versarla all'Erario entro il 16 del mese successivo al pagamento (tramite F24, codice tributo 1040)
3. Rilasciare la Certificazione Unica (CU) al percipiente

### 4.2 Percentuali e Base di Calcolo

| Tipologia | Aliquota | Base di calcolo | Ritenuta effettiva |
|-----------|----------|----------------|-------------------|
| **Professionisti** (lavoro autonomo) | 20% | 100% del compenso (imponibile) | 20% dell'imponibile |
| **Agenti senza dipendenti** | 23% | 50% delle provvigioni | 11,50% delle provvigioni |
| **Agenti con dipendenti** | 23% | 20% delle provvigioni | 4,60% delle provvigioni |
| **Prestazioni occasionali** | 20% | 100% del compenso | 20% del compenso |
| **Professionisti non residenti** | 30% | 100% del compenso (a titolo definitivo) | 30% del compenso |

### 4.3 Regola Fondamentale: Base di Calcolo

**La ritenuta d'acconto si calcola SEMPRE sull'imponibile (al netto dell'IVA), MAI sul totale lordo comprensivo di IVA.**

Esempio corretto:

```
Compenso professionale (imponibile):  1.000,00 EUR
IVA 22%:                                220,00 EUR
Totale fattura:                       1.220,00 EUR
Ritenuta d'acconto 20% di 1.000:      -200,00 EUR
Netto a pagare:                       1.020,00 EUR
```

### 4.4 Esclusioni dalla Ritenuta

La ritenuta d'acconto **NON si applica** a:

- Soggetti in **regime forfettario** (devono indicarlo in fattura)
- Aziende (SRL, SPA, SRLS, SNC, SAS quando pagano fornitori non professionisti)
- Pubblica Amministrazione (per le fatture soggette a split payment, poiche si applicherebbe un doppio prelievo)

### 4.5 Interazione Split Payment e Ritenuta

Split payment e ritenuta d'acconto sono **mutuamente esclusivi**: quando il compenso e soggetto a ritenuta d'acconto, lo split payment non si applica, e viceversa. Questo evita il doppio prelievo a danno del fornitore.

---

## 5. Split Payment (Art. 17-ter DPR 633/72)

### 5.1 Meccanismo

Il fornitore emette fattura con IVA esposta ma **non la incassa**. La PA (o altro soggetto obbligato):

- Paga al fornitore **solo l'imponibile**
- Versa l'IVA **direttamente all'Erario**

In fattura va indicato: "Scissione dei pagamenti - Art. 17-ter DPR 633/72" e nella fattura elettronica l'esigibilita IVA va impostata su "S" (split payment).

### 5.2 Soggetti Obbligati

- Pubbliche Amministrazioni (Stato, Regioni, Province, Comuni, enti pubblici)
- Societa controllate da enti pubblici (partecipazione > 70%)
- Societa quotate nell'indice FTSE MIB (fino al 30/06/2025)
- Fondazioni ed enti prevalentemente finanziati da fondi pubblici

**Validita:** prorogato fino al 30 giugno 2026 (Decisione UE 2023/1552).

### 5.3 Calcolo del Netto a Pagare con Split Payment

```
Imponibile:         1.000,00 EUR
IVA 22%:              220,00 EUR   --> versata dalla PA all'Erario
Netto a pagare:     1.000,00 EUR   --> pagato dalla PA al fornitore
```

---

## 6. Numerazione delle Fatture

### 6.1 Regole (Art. 21, comma 2, lett. b, DPR 633/72)

- La fattura deve contenere un **numero progressivo** che la identifichi **in modo univoco**
- Non possono esistere due fatture con lo stesso numero nello stesso ambito

### 6.2 Formati Ammessi

Dal 2013 (Circolare Agenzia delle Entrate n. 1/E del 25/01/2013), la norma non richiede piu la progressivita "per anno solare". E sufficiente l'univocita. Formati validi:

1. **Numerazione continua senza azzeramento**: 1, 2, 3... 1548, 1549...
2. **Numerazione con anno** (la piu diffusa): 1/2026, 2/2026 oppure 2026/001, 2026/002
3. **Sotto-serie (sezionali)**: 1/2026/A, 1/2026/B (per registri sezionali diversi)
4. **Formato alfanumerico**: FE-2026-0001

### 6.3 Azzeramento a Inizio Anno

L'azzeramento della numerazione al 1 gennaio e **facoltativo** ma **consigliato**. Se si adotta, e necessario includere l'anno nella numerazione per garantire l'univocita.

### 6.4 Regola di Progressivita

All'interno dello stesso anno e della stessa serie, i numeri devono essere **strettamente crescenti**. Non e ammesso saltare numeri (anche se non produce sanzioni dirette, puo generare presunzione di fattura omessa in sede di accertamento).

---

## 7. Ciclo di Vita della Fattura

### 7.1 Stati e Significato Fiscale

| Stato | Significato | Vincoli |
|-------|-------------|---------|
| **Bozza** (Draft) | Documento in preparazione, non ha ancora valore fiscale | Puo essere modificata liberamente. Non ha numero assegnato. |
| **Emessa** (Issued) | Fattura formalmente emessa, numero assegnato | Ha valore fiscale. Da questo momento, NON dovrebbe essere modificata nei dati sostanziali (importi, aliquote, dati del cliente). |
| **Inviata** (Sent) | Fattura trasmessa al cliente (o allo SDI per la fatturazione elettronica) | Idem. L'invio allo SDI deve avvenire entro 12 giorni dalla data fattura. |
| **Pagata** (Paid) | Il cliente ha saldato la fattura | Stato finale positivo. |
| **Scaduta** (Overdue) | La data di scadenza e superata senza pagamento | Puo prevedere interessi moratori (Art. 5 D.Lgs. 231/2002). |
| **Annullata** (Cancelled) | Fattura annullata | Se gia emessa, l'annullamento fiscale richiede l'emissione di una **nota di credito**. |

### 7.2 Regola Fondamentale sull'Immutabilita

**Una fattura emessa (con numero assegnato) non puo essere modificata nei suoi elementi sostanziali.** Per correggere errori o variazioni, la normativa prevede:

- **Nota di credito** (TD04) per variazioni in diminuzione
- **Nota di debito** (TD05) per variazioni in aumento

> **Stato attuale nell'applicazione:** Le fatture gia emesse possono essere modificate liberamente tramite PUT, senza alcun vincolo. Questo viola il principio di immutabilita della fattura emessa. Inoltre, le note di credito/debito non sono gestite.

### 7.3 Transizioni di Stato Ammesse

```
Draft --> Issued    (emissione: assegnazione numero progressivo)
Draft --> Cancelled (annullamento della bozza)
Issued --> Sent     (invio al cliente / trasmissione SDI)
Issued --> Cancelled (annullamento: richiede nota di credito)
Sent --> Paid       (pagamento ricevuto)
Sent --> Overdue    (scadenza superata senza pagamento)
Sent --> Cancelled  (annullamento: richiede nota di credito)
Overdue --> Paid    (pagamento ricevuto dopo scadenza)
Overdue --> Cancelled (annullamento: richiede nota di credito)
```

Stati finali (nessuna transizione in uscita): **Paid**, **Cancelled**.

---

## 8. Termini di Emissione

### 8.1 Fattura Immediata (Art. 21, comma 4, DPR 633/72)

Deve essere emessa **entro 12 giorni** dall'effettuazione dell'operazione.

La data della fattura deve corrispondere alla data dell'operazione, anche se la trasmissione allo SDI avviene nei giorni successivi (entro i 12 giorni).

### 8.2 Fattura Differita (Art. 21, comma 4, lett. a)

Puo essere emessa entro il **giorno 15 del mese successivo** a quello di effettuazione dell'operazione, quando:

- La cessione di beni risulta da un **DDT** (Documento di Trasporto) o altro documento idoneo
- La prestazione di servizi e individuabile attraverso documentazione idonea

### 8.3 Momento di Effettuazione dell'Operazione (Art. 6 DPR 633/72)

| Tipo operazione | Momento di effettuazione |
|----------------|--------------------------|
| Cessione di beni mobili | Consegna o spedizione |
| Cessione di beni immobili | Stipulazione dell'atto |
| Prestazione di servizi | Pagamento del corrispettivo |
| Pagamento anticipato (acconto) | Al momento del pagamento, limitatamente all'importo |
| Emissione anticipata della fattura | Al momento dell'emissione |

### 8.4 Esigibilita dell'IVA

- **Esigibilita immediata** (regola generale): l'IVA diventa esigibile al momento dell'effettuazione
- **Esigibilita differita**: per cessioni/prestazioni verso enti pubblici, l'IVA diventa esigibile al momento del pagamento (dal 2015 sostituita dallo split payment per la PA)
- Anche con fattura differita, l'IVA va computata nella **liquidazione del mese di effettuazione**, non di emissione

---

## 9. Note di Credito e Note di Debito (Art. 26 DPR 633/72)

### 9.1 Nota di Credito (Variazione in Diminuzione)

**Quando emetterla:**

- Nullita, annullamento, revoca, risoluzione, rescissione del contratto
- Applicazione di abbuoni o sconti previsti contrattualmente
- Reso di merce
- Rettifica di inesattezze della fatturazione
- Mancato pagamento per procedure concorsuali o esecutive infruttuose

**Termine:** entro 1 anno dall'operazione per variazioni da accordo tra le parti. Nessun limite per variazioni da nullita, annullamento, revoca, risoluzione, rescissione o rettifica di errori.

**L'emissione e facoltativa** (il cedente puo decidere di non recuperare l'IVA), ma se emessa, il cessionario e obbligato a registrarla.

### 9.2 Nota di Debito (Variazione in Aumento)

Da emettere quando si verificano aumenti dell'imponibile o dell'imposta rispetto alla fattura originaria. L'emissione e **obbligatoria** e senza limitazioni temporali.

### 9.3 Requisiti Formali

- Stessi requisiti della fattura (art. 21 DPR 633/72)
- Riferimento alla fattura originaria
- Indicazione della differenza degli importi

**Codici tipo documento SDI:** TD04 (nota di credito), TD05 (nota di debito), TD08 (NC semplificata), TD09 (ND semplificata)

---

## 10. Fatturazione Elettronica

### 10.1 Obbligo

| Data | Soggetti obbligati |
|------|-------------------|
| 31 marzo 2015 | Verso la Pubblica Amministrazione |
| 1 gennaio 2019 | Tutti i soggetti IVA (B2B e B2C) |
| 1 luglio 2022 | Forfettari con ricavi > 25.000 EUR |
| 1 gennaio 2024 | Tutti i forfettari, senza eccezioni |

**Eccezione permanente:** divieto di fattura elettronica per prestazioni sanitarie verso consumatori finali (tutela dati sanitari).

### 10.2 Formato FatturaPA (XML)

Il formato obbligatorio e il tracciato **FatturaPA** in XML. Principali tipi documento:

| Codice | Tipo documento |
|--------|---------------|
| TD01 | Fattura immediata |
| TD02 | Acconto/anticipo su fattura |
| TD04 | Nota di credito |
| TD05 | Nota di debito |
| TD06 | Parcella (professionisti) |
| TD07 | Fattura semplificata |
| TD24 | Fattura differita |

### 10.3 Codice Destinatario e PEC

| Tipo destinatario | Identificativo | Formato |
|-------------------|---------------|---------|
| Privato (B2B) con SDI | Codice Destinatario | 7 caratteri alfanumerici |
| Pubblica Amministrazione | Codice Univoco Ufficio (CUU/IPA) | 6 caratteri alfanumerici |
| Destinatario con PEC | Codice "0000000" + indirizzo PEC | 7 zeri + PEC |
| Consumatore finale | Codice "0000000" | 7 zeri (fattura nel cassetto fiscale) |

### 10.4 CIG e CUP per la PA (Art. 25 DL 66/2014, Legge 136/2010)

- **CIG** (Codice Identificativo di Gara): 10 caratteri alfanumerici. Obbligatorio su tutte le fatture verso la PA.
- **CUP** (Codice Unico di Progetto): 15 caratteri alfanumerici. Obbligatorio per opere pubbliche e progetti di investimento.
- **Senza CIG/CUP (ove obbligatori), la PA non puo procedere al pagamento.**

### 10.5 Conservazione Digitale

- Durata: almeno **10 anni** dalla data dell'ultima registrazione
- Deve garantire autenticita, integrita, leggibilita e reperibilita
- L'Agenzia delle Entrate offre un servizio di conservazione gratuito

---

## 11. Imposta di Bollo (Art. 13 DPR 642/72)

### 11.1 Quando e Obbligatoria

L'imposta di bollo di **2,00 EUR** per fattura e dovuta quando:

- L'operazione e **esente IVA** (art. 10)
- L'operazione e **fuori campo IVA** (artt. 2, 3, 5, 7)
- L'operazione e **esclusa** dalla base imponibile (art. 15)
- L'operazione e **non imponibile** senza diritto alla detrazione (regime forfettario)
- **E** l'importo della fattura (per la parte non soggetta a IVA) supera **77,47 EUR**

### 11.2 Fatture Miste

Per fatture con importi sia soggetti a IVA sia esenti/fuori campo, il bollo si applica solo se la **porzione non soggetta a IVA supera i 77,47 EUR**.

### 11.3 Nella Fattura Elettronica

- Si indica il campo "BolloVirtuale" = "SI" nel tracciato XML
- Il versamento avviene **trimestralmente** tramite F24

### 11.4 Chi Paga

L'obbligo grava sul **soggetto che emette la fattura**. Il costo puo essere addebitato al cliente.

> **Stato attuale nell'applicazione:** L'imposta di bollo non e gestita.

---

## 12. Sconti

### 12.1 Tipologie

- **Sconto percentuale**: percentuale applicata sull'importo lordo della riga
- **Sconto fisso (abbuono)**: importo assoluto detratto dal totale della riga
- **Sconto incondizionato**: riduce direttamente la base imponibile IVA
- **Sconto condizionato** (es. per pagamento anticipato): non riduce la base imponibile al momento dell'emissione

### 12.2 Nella Fattura Elettronica

Il tracciato FatturaPA prevede un blocco specifico per gli sconti/maggiorazioni (ScontoMaggiorazione) che puo essere:

- A livello di singola riga (blocco 2.2.1.10)
- A livello dell'intero documento (blocco 2.1.1.8)

---

## 13. Condizioni di Pagamento

### 13.1 Elementi Obbligatori nella Fattura Elettronica

Il tracciato FatturaPA prevede il blocco DatiPagamento (2.4) con:

- **Modalita di pagamento**: bonifico (MP05), RiBa (MP12), contanti (MP01), carta di credito (MP08), ecc.
- **Coordinate bancarie (IBAN)**: del beneficiario del pagamento
- **Termini**: data di scadenza, giorni di pagamento
- **Condizioni**: pagamento completo (TP02), pagamento a rate (TP01), anticipo (TP03)

### 13.2 Termini Commerciali Comuni in Italia

| Condizione | Descrizione |
|------------|-------------|
| 30 gg DF | 30 giorni data fattura |
| 30 gg FM | 30 giorni fine mese |
| 60 gg DF | 60 giorni data fattura |
| 60 gg FM | 60 giorni fine mese |
| Rimessa diretta | Pagamento alla consegna o a vista |
| RiBa 30/60/90 | Ricevuta bancaria con scadenze multiple |

---

## 14. Riepilogo: Gap tra Normativa e Implementazione Attuale

### Bug Critici

| # | Problema | Impatto |
|---|----------|---------|
| 1 | **Ritenuta calcolata su SubTotal (imponibile+IVA) anziche su ImponibileTotal** | Importi errati. Es: su 1.000 EUR di imponibile con IVA 22%, la ritenuta e 244 EUR invece di 200 EUR. |
| 2 | **Numerazione non resetta al cambio anno** | Se l'ultima fattura del 2025 e "2025/042", la prima del 2026 sara "2026/043" invece di "2026/001". |

### Lacune Normative

| # | Elemento mancante | Riferimento normativo |
|---|-------------------|----------------------|
| 3 | Dati dell'emittente (cedente/prestatore) | Art. 21, co. 2, lett. c-d, DPR 633/72 |
| 4 | Aliquota IVA 5% | Tabella A, Parte II-bis, DPR 633/72 |
| 5 | Codici Natura IVA (N1-N7) per operazioni a 0% | Specifiche FatturaPA |
| 6 | Split payment per Pubblica Amministrazione | Art. 17-ter DPR 633/72 |
| 7 | Regime forfettario | Legge 190/2014, commi 54-89 |
| 8 | Note di credito e note di debito | Art. 26 DPR 633/72 |
| 9 | Fatturazione elettronica (XML, SDI, Codice Destinatario) | DL 119/2018, DL 127/2015 |
| 10 | Codice Univoco Ufficio, CIG, CUP per PA | Art. 25 DL 66/2014, L. 136/2010 |
| 11 | Imposta di bollo | Art. 13 DPR 642/72 |
| 12 | Condizioni e modalita di pagamento | Blocco 2.4 tracciato FatturaPA |
| 13 | Reverse charge | Art. 17 DPR 633/72 |
| 14 | Immutabilita fattura emessa (le fatture emesse possono essere modificate via PUT) | Art. 21 DPR 633/72 |
| 15 | Validazione Codice Fiscale | - |
| 16 | Validazione campi obbligatori incompleta (Quantity > 0, UnitPrice >= 0, RagioneSociale non vuota) | Art. 21 DPR 633/72 |
| 17 | Conservazione digitale a norma | Art. 39 DPR 633/72, DM 17/06/2014 |

### Validazioni Mancanti sul Modello Client

| Campo | Validazione necessaria |
|-------|----------------------|
| `RagioneSociale` | Obbligatorio, non vuoto |
| `CodiceFiscale` | 16 caratteri alfanumerici (persone fisiche) o 11 cifre (persone giuridiche) |
| `Email` | Formato email valido |
| `Address.PostalCode` | Esattamente 5 cifre (CAP italiano) |
| `Address.Province` | Esattamente 2 lettere maiuscole (sigla provincia) |
| Per PA: `CodiceUnivocoUfficio` | 6 caratteri alfanumerici (manca il campo) |
| Per PA: `CIG` | 10 caratteri alfanumerici (manca il campo) |

### Validazioni Mancanti sul Modello InvoiceItem

| Campo | Validazione necessaria |
|-------|----------------------|
| `Quantity` | Deve essere > 0 |
| `UnitPrice` | Deve essere >= 0 |
| `IvaRate` | Deve essere un valore valido dell'enum |

### Validazioni Mancanti sul Modello Invoice

| Regola | Descrizione |
|--------|-------------|
| Immutabilita post-emissione | Una fattura con stato != Draft non dovrebbe poter essere modificata nei dati sostanziali |
| Coerenza data fattura | La data fattura non dovrebbe essere nel futuro |
| Almeno una riga | Una fattura dovrebbe contenere almeno un item |
| Ricalcolo all'emissione | L'endpoint `/issue` dovrebbe ricalcolare i totali prima di emettere |
