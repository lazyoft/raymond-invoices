# 🐛 Bug Report - Sistema di Fatturazione Elettronica Italiana

**Data Audit:** 14 Febbraio 2026

**Auditor:** Claude Code (con skill `/implement-feature`)

**Normativa di Riferimento:** DPR 633/72, DPR 600/73, DPR 642/72, DL 119/2018

---

## 🔴 Bug Critici - BLOCKERS (Violazione Normativa Fiscale)

### Bug #1: Ritenuta d'Acconto Calcolata su SubTotal invece di ImponibileTotal

**Severità:** 🔴 CRITICA

**Impatto Fiscale:** **ALTO** - Importi errati in fattura, violazione normativa

**Riferimento Normativo:** Art. 25 DPR 600/73

**File:** `src/Fatturazione.Domain/Services/InvoiceCalculationService.cs:89-92`

**Descrizione:**

La ritenuta d'acconto viene calcolata su `invoice.SubTotal` (imponibile + IVA) invece che su `invoice.ImponibileTotal` (solo imponibile).

**Codice Errato:**

```csharp
// Line 89-92
return _ritenutaService.CalculateRitenuta(
    invoice.SubTotal,  // ❌ ERRATO - include l'IVA
    invoice.Client.RitenutaPercentage
);
```

**Dovrebbe Essere:**

```csharp
return _ritenutaService.CalculateRitenuta(
    invoice.ImponibileTotal,  // ✅ CORRETTO - solo imponibile
    invoice.Client.RitenutaPercentage
);
```

**Esempio Impatto:**

```text
Imponibile:     1.000,00 EUR
IVA 22%:          220,00 EUR
SubTotal:       1.220,00 EUR

❌ Ritenuta 20% di 1.220 = 244,00 EUR (SBAGLIATO)
✅ Ritenuta 20% di 1.000 = 200,00 EUR (CORRETTO)

Differenza: 44 EUR in più trattenuti al fornitore
Netto a pagare errato: 976 EUR invece di 1.020 EUR
```

**Test che Documentano il Bug:**

- `InvoiceCalculationServiceTests.cs:297-330` - Test marcato `[Trait("Category", "KnownBug")]`
- `InvoiceCalculationServiceTests.cs:338-365` - Verifica che viene passato SubTotal (1220) invece di ImponibileTotal (1000)

---

### Bug #2: Numerazione Fattura Non Si Resetta al Cambio Anno

**Severità:** 🔴 CRITICA

**Impatto Fiscale:** **MEDIO** - Numerazione non conforme, presunzione di fatture omesse in sede di accertamento

**Riferimento Normativo:** Art. 21, comma 2, lett. b, DPR 633/72 + Circolare AdE 1/E/2013

**File:** `src/Fatturazione.Domain/Services/InvoiceNumberingService.cs:30-35`

**Descrizione:**

Il metodo `GenerateNextInvoiceNumber` incrementa sempre il sequence number senza controllare se l'anno è cambiato. Se l'ultima fattura del 2025 è `2025/042`, la prima del 2026 sarà `2026/043` invece di `2026/001`.

**Codice Errato:**

```csharp
// Line 30-35
int lastYear = GetYearFromInvoiceNumber(lastInvoiceNumber);
int lastSequence = GetSequenceFromInvoiceNumber(lastInvoiceNumber);

int nextSequence = lastSequence + 1;  // ❌ Non controlla cambio anno

return $"{currentYear}/{nextSequence:D3}";
```

**Dovrebbe Essere:**

```csharp
int lastYear = GetYearFromInvoiceNumber(lastInvoiceNumber);
int lastSequence = GetSequenceFromInvoiceNumber(lastInvoiceNumber);

int nextSequence = (currentYear > lastYear) ? 1 : lastSequence + 1;  // ✅ Reset al nuovo anno

return $"{currentYear}/{nextSequence:D3}";
```

**Esempio Impatto:**

```text
Scenario: Ultima fattura 2025
  Ultima fattura: 2025/042
  Prima fattura 2026:
    ❌ Generato: 2026/043 (SBAGLIATO)
    ✅ Atteso:   2026/001 (CORRETTO)
```

---

## 🟠 Bug Maggiori - Violazioni Normative (Non Bloccanti ma Serie)

### Bug #3: Immutabilità Fattura Emessa Non Rispettata

**Severità:** 🟠 ALTA

**Impatto Fiscale:** **ALTO** - Una fattura già emessa non può essere modificata nei dati sostanziali

**Riferimento Normativo:** Art. 21 DPR 633/72

**File:** `src/Fatturazione.Api/Endpoints/InvoiceEndpoints.cs:123-169`

**Descrizione:**

L'endpoint `PUT /invoices/{id}` (UpdateInvoice) NON verifica lo stato della fattura prima di permettere modifiche. Una fattura con status `Issued`, `Sent`, `Paid`, o `Overdue` può essere modificata liberamente, violando il principio di immutabilità fiscale.

**Codice Mancante:**

```csharp
// Dopo la riga 139 dovrebbe esserci:
if (existing.Status != InvoiceStatus.Draft)
{
    return Results.ValidationProblem(new Dictionary<string, string[]>
    {
        { "Status", new[] { "Cannot modify an issued invoice. Use nota di credito/debito instead." } }
    });
}
```

**Cosa Permette Attualmente:**

- Modificare importi, aliquote IVA, descrizioni di fatture già emesse
- Cambiare il cliente di una fattura già inviata
- Alterare la data fattura di documenti già registrati

**Normativa:**

Una fattura emessa (con numero assegnato) è **immutabile**. Per correggere errori serve emettere:

- **Nota di Credito** (TD04) per variazioni in diminuzione
- **Nota di Debito** (TD05) per variazioni in aumento

---

### Bug #4: Aliquota IVA 5% Mancante

**Severità:** 🟠 MEDIA

**Impatto Fiscale:** **MEDIO** - Sistema incompleto per fatture con aliquota intermedia

**Riferimento Normativo:** Tabella A, Parte II-bis, DPR 633/72

**File:** `src/Fatturazione.Domain/Models/IvaRate.cs:6-31`

**Descrizione:**

L'enum `IvaRate` include solo 22%, 10%, 4%, 0%. Manca l'aliquota **5%** (intermedia), che si applica a:

- Prestazioni socio-sanitarie di cooperative sociali
- Gas metano civile (primi 480 mc/anno)
- Alcune categorie di erbe aromatiche fresche

**Codice Attuale:**

```csharp
public enum IvaRate
{
    Standard = 22,
    Reduced = 10,
    SuperReduced = 4,
    Zero = 0
    // ❌ Manca: Intermediate = 5
}
```

---

### Bug #5: Split Payment per PA Non Gestito

**Severità:** 🟠 ALTA

**Impatto Fiscale:** **ALTO** - Le fatture verso PA devono applicare lo split payment

**Riferimento Normativo:** Art. 17-ter DPR 633/72

**File:** `src/Fatturazione.Domain/Models/Client.cs`

**Descrizione:**

Il modello `Client` NON ha campi per gestire lo split payment. Per clienti PA, l'IVA non viene incassata dal fornitore ma versata direttamente dalla PA all'Erario.

**Campi Mancanti:**

- `bool SubjectToSplitPayment` (analogo a `SubjectToRitenuta`)
- `string CodiceUnivocoUfficio` (6 caratteri alfanumerici, obbligatorio per PA)
- `string? CIG` (Codice Identificativo di Gara, 10 caratteri)
- `string? CUP` (Codice Unico di Progetto, 15 caratteri)

**Impatto:**

Senza split payment, il calcolo del `TotalDue` è errato per fatture PA:

```text
Imponibile:     1.000,00 EUR
IVA 22%:          220,00 EUR

❌ Senza split payment: TotalDue = 1.220 EUR (il fornitore si aspetta 1.220)
✅ Con split payment:   TotalDue = 1.000 EUR (la PA paga solo l'imponibile)
```

**Note Importanti:**

- Split payment e ritenuta sono **mutuamente esclusivi**
- Per PA con split payment, la ritenuta NON si applica

---

## 🟡 Validazioni Mancanti - Violazioni Art. 21 DPR 633/72

### Bug #6: InvoiceValidator - Validazioni Campi Obbligatori Mancanti

**Severità:** 🟡 MEDIA

**Impatto Fiscale:** **MEDIO** - Dati fattura incompleti o non validi

**Riferimento Normativo:** Art. 21, comma 2, DPR 633/72

**File:** `src/Fatturazione.Domain/Validators/InvoiceValidator.cs:55-65`

**Validazioni Mancanti su InvoiceItem:**

```csharp
// ❌ MANCA: validazione Quantity > 0
if (item.Quantity <= 0)
{
    errors.Add($"Item {itemNumber}: Quantity deve essere maggiore di 0");
}

// ❌ MANCA: validazione UnitPrice >= 0
if (item.UnitPrice < 0)
{
    errors.Add($"Item {itemNumber}: UnitPrice non può essere negativo");
}
```

**Validazioni Mancanti su Invoice:**

```csharp
// ❌ MANCA: almeno un item
if (invoice.Items == null || invoice.Items.Count == 0)
{
    errors.Add("La fattura deve contenere almeno un item");
}

// ❌ MANCA: data fattura non nel futuro
if (invoice.InvoiceDate > DateTime.Now.Date)
{
    errors.Add("InvoiceDate non può essere nel futuro");
}
```

---

### Bug #7: Client - Validazioni Dati Anagrafici Mancanti

**Severità:** 🟡 MEDIA

**Impatto Fiscale:** **MEDIO**

**Riferimento Normativo:** Art. 21, comma 2, lett. e-f, DPR 633/72

**File:** Manca un `ClientValidator.cs`

**Validazioni da Aggiungere:**

| Campo | Validazione Necessaria |
| ----- | ---------------------- |
| `RagioneSociale` | ❌ Obbligatorio, non vuoto |
| `CodiceFiscale` | ❌ 16 caratteri alfanumerici (persone fisiche) o 11 cifre (persone giuridiche) |
| `Email` | ❌ Formato email valido |
| `Address.PostalCode` | ❌ Esattamente 5 cifre numeriche (CAP italiano) |
| `Address.Province` | ❌ Esattamente 2 lettere maiuscole (sigla provincia, es. "MI", "RM") |
| `Address.Street` | ❌ Obbligatorio, non vuoto |
| `Address.City` | ❌ Obbligatorio, non vuoto |

**Note:**

- Il validator `PartitaIvaValidator.cs` è già implementato correttamente con l'algoritmo di checksum
- Manca un validator per il `CodiceFiscale`

---

### Bug #8: Ricalcolo Totali Non Eseguito all'Emissione

**Severità:** 🟡 MEDIA

**Impatto Fiscale:** **MEDIO** - Totali non aggiornati in fatture emesse

**File:** `src/Fatturazione.Api/Endpoints/InvoiceEndpoints.cs:194-224`

**Descrizione:**

L'endpoint `/issue` assegna il numero fattura e cambia lo stato a `Issued`, ma **NON ricalcola i totali**. Se l'utente ha modificato gli items dopo l'ultimo calcolo, i totali sono stale.

**Codice Mancante (dopo riga 219):**

```csharp
// Load client for calculations
invoice.Client = await clientRepository.GetByIdAsync(invoice.ClientId);

// Recalculate totals before issuing
calculationService.CalculateInvoiceTotals(invoice);
```

---

## 📋 Lacune Normative - Feature Mancanti

### Gap #1: Dati Emittente Mancanti

**Severità:** 🟡 BASSA (per ora - sistema incompleto)

**Riferimento:** Art. 21, co. 2, lett. c-d, DPR 633/72

**Descrizione:**

Ogni fattura DEVE contenere i dati del **cedente/prestatore** (chi emette):

- Denominazione/ragione sociale
- Sede legale
- Partita IVA

**Attualmente:**

Il sistema gestisce solo i dati del **cliente** (cessionario/committente), non dell'emittente.

---

### Gap #2: Note di Credito e Note di Debito Non Gestite

**Severità:** 🟠 MEDIA

**Riferimento:** Art. 26 DPR 633/72

**Descrizione:**

Per correggere/annullare fatture emesse servono:

- **Nota di Credito** (TD04) - variazioni in diminuzione
- **Nota di Debito** (TD05) - variazioni in aumento

**Attualmente:**

Non c'è supporto per emettere note di credito/debito. Lo stato `Cancelled` esiste ma non genera nota di credito.

---

### Gap #3: Imposta di Bollo Non Gestita

**Severità:** 🟡 MEDIA

**Riferimento:** Art. 13 DPR 642/72

**Descrizione:**

Per fatture **esenti IVA** o **fuori campo IVA** con importo > 77,47 EUR, è dovuta l'imposta di bollo di **2,00 EUR**.

**Attualmente:**

L'imposta di bollo non è calcolata né indicata.

---

### Gap #4: Regime Forfettario Non Gestito

**Severità:** 🟡 MEDIA

**Riferimento:** Legge 190/2014, commi 54-89

**Descrizione:**

I contribuenti in regime forfettario:

- NON addebitano IVA
- NON sono soggetti a ritenuta d'acconto
- Devono indicare in fattura: "Operazione effettuata ai sensi dell'art. 1, commi 54-89, Legge n. 190/2014"

**Attualmente:**

Non c'è modo di marcare un cliente come forfettario.

---

### Gap #5: Codici Natura IVA Non Gestiti

**Severità:** 🟡 MEDIA

**Riferimento:** Specifiche Tecniche FatturaPA

**Descrizione:**

Quando l'aliquota è 0%, nella fattura elettronica è **obbligatorio** specificare il codice natura (N1-N7):

- N1: Escluse ex art. 15
- N2.1, N2.2: Non soggette
- N3.1-N3.6: Non imponibili (esportazioni, intracomunitarie, ecc.)
- N4: Esenti
- N6.x: Reverse charge
- N7: IVA assolta in altro stato UE

**Attualmente:**

L'aliquota `IvaRate.Zero` esiste ma senza codice natura.

---

## 📊 Riepilogo Priorità

### 🔴 URGENTI (Da Fixare Subito)

1. **Bug #1** - Ritenuta calcolata su SubTotal invece di ImponibileTotal
2. **Bug #2** - Numerazione non resetta al cambio anno
3. **Bug #3** - Immutabilità fattura emessa non rispettata

### 🟠 IMPORTANTI (Da Fixare Prima di Produzione)

4. **Bug #5** - Split payment per PA non gestito
5. **Bug #4** - Aliquota IVA 5% mancante
6. **Bug #6** - Validazioni campi obbligatori mancanti
7. **Bug #7** - Validazioni dati anagrafici mancanti
8. **Bug #8** - Ricalcolo totali non eseguito all'emissione

### 🟡 MIGLIORAMENTI (Feature Complete)

9. Gap #2 - Note di credito/debito
10. Gap #3 - Imposta di bollo
11. Gap #4 - Regime forfettario
12. Gap #5 - Codici natura IVA
13. Gap #1 - Dati emittente

---

## 🧪 Test Coverage

**Test che Documentano Bug Esistenti:**

- ✅ `InvoiceCalculationServiceTests.cs:297-330` - Bug #1 ritenuta (marcato `KnownBug`)
- ✅ `InvoiceCalculationServiceTests.cs:338-365` - Bug #1 verifica parametro SubTotal
- ❌ Nessun test per Bug #2 (numerazione cambio anno)
- ❌ Nessun test per Bug #3 (immutabilità)

**Copertura Test:**

- ✅ `InvoiceCalculationService` - ben testato
- ✅ `RitenutaService` - ben testato
- ✅ `PartitaIvaValidator` - presumibilmente testato
- ❌ `InvoiceNumberingService` - test mancanti per cambio anno
- ❌ `InvoiceValidator` - test parziali
- ❌ `InvoiceEndpoints` - test esistenti ma non coprono immutabilità

---

## 📖 Riferimenti Normativi Completi

- **DPR 633/1972** - Testo Unico IVA
- **DPR 600/1973** - Ritenute d'acconto
- **DPR 642/1972** - Imposta di bollo
- **DL 119/2018** - Fatturazione elettronica obbligatoria
- **DL 127/2015** - Fatturazione elettronica PA
- **Legge 190/2014** - Regime forfettario
- **Art. 17-ter DPR 633/72** - Split payment (prorogato al 30/06/2026)
- **Circolare AdE 1/E/2013** - Numerazione fatture

---

## 🎯 Prossimi Passi Raccomandati

### Fase 1: Fix Bug Critici (Priorità Massima)

1. Fixare Bug #1 (ritenuta) - 1 riga di codice + aggiornare test
2. Fixare Bug #2 (numerazione) - 1 riga di codice + aggiungere test
3. Fixare Bug #3 (immutabilità) - aggiungere validazione in UpdateInvoice

### Fase 2: Completare Validazioni (Prima di Produzione)

4. Implementare Bug #6 (validazioni Invoice/InvoiceItem)
5. Implementare Bug #7 (validazioni Client)
6. Implementare Bug #8 (ricalcolo in /issue)

### Fase 3: Split Payment e Feature Mancanti

7. Implementare Bug #5 (split payment PA + CIG/CUP)
8. Aggiungere Gap #2 (note di credito/debito)
9. Aggiungere Gap #3 (imposta di bollo)

### Fase 4: Completamento Normativo

10. Implementare Gap #4 (regime forfettario)
11. Implementare Gap #5 (codici natura IVA)
12. Aggiungere Bug #4 (aliquota IVA 5%)
13. Aggiungere Gap #1 (dati emittente)

---

**Fine Bug Report**

**Generato il:** 14 Febbraio 2026

**Tool:** Claude Code `/implement-feature` skill
