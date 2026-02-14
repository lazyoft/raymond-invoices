# Esempi di Implementazione

Questa sezione mostra scenari comuni di utilizzo della skill con esempi concreti.

## Esempio 1: Aggiungere Split Payment per Fatture PA

**Richiesta utente:**
"Aggiungi il supporto per lo split payment nelle fatture alla Pubblica Amministrazione"

**Flusso di lavoro:**

1. **Analisi del dominio**
   - Leggi sezione 4.3 di `fiscal-rules.md` (Split Payment)
   - Regola chiave: La PA versa l'IVA direttamente all'Erario, quindi il totale fattura non include l'IVA
   - Incompatibilità: Split payment è incompatibile con ritenuta d'acconto

2. **Analisi del codice**
   - Modello `Invoice`: aggiungere campo `bool IsSplitPayment`
   - `InvoiceCalculationService`: modificare `CalculateTotal()` per escludere IVA se split payment
   - Validator: assicurarsi che split payment + ritenuta = errore

3. **Piano di implementazione**
   ```
   Regole fiscali:
   - Art. 17-ter DPR 633/72: Split payment per PA
   - Totale = Imponibile (senza IVA per split payment)

   Modifiche:
   1. Invoice.cs - aggiungi IsSplitPayment bool
   2. InvoiceCalculationService.cs - modifica CalculateTotal()
   3. InvoiceValidator.cs - valida incompatibilità con ritenuta

   Test:
   1. Test_SplitPayment_ExcludesVatFromTotal
   2. Test_SplitPayment_IncompatibleWithRitenuta
   3. Test_PAClient_RequiresSplitPayment
   ```

4. **Implementazione**
   - Codice conforme alla normativa
   - Test verificano la regola fiscale, non solo il calcolo
   - Esegui `dotnet test` prima di completare

**Risultato:** Fatture PA corrette con IVA esposta ma non incassata

---

## Esempio 2: Implementare Regime Forfettario

**Richiesta utente:**
"Implementa il supporto per clienti in regime forfettario"

**Flusso di lavoro:**

1. **Analisi del dominio**
   - Leggi sezione 2.3 di `fiscal-rules.md` (Regime Forfettario)
   - Regole chiave:
     - Nessuna IVA applicata
     - Nessuna ritenuta d'acconto subita
     - Causale obbligatoria in fattura: "Operazione senza applicazione IVA ai sensi dell'art. 1 c. 58 L. 190/2014"

2. **Analisi del codice**
   - Modello `Client`: aggiungere campo `bool IsRegimeForfettario`
   - `InvoiceCalculationService`:
     - Se regime forfettario → IVA = 0
     - Se regime forfettario → Ritenuta = 0
   - `Invoice`: aggiungere campo `Causale?` per la nota legale

3. **Piano di implementazione**
   ```
   Regole fiscali:
   - Legge 190/2014, commi 54-89: Regime forfettario
   - No IVA, no ritenuta, causale obbligatoria

   Modifiche:
   1. Client.cs - aggiungi IsRegimeForfettario bool
   2. Invoice.cs - aggiungi Causale string?
   3. InvoiceCalculationService.cs - azzera IVA e ritenuta se forfettario
   4. InvoiceValidator.cs - verifica causale presente se forfettario

   Test:
   1. Test_RegimeForfettario_NoVat
   2. Test_RegimeForfettario_NoRitenuta
   3. Test_RegimeForfettario_RequiresCausale
   4. Test_RegimeForfettario_TotalEqualsImponibile
   ```

4. **Implementazione**
   - Gestisci il caso speciale nei calcoli
   - Valida la presenza della causale
   - Test coprono tutti i casi: IVA=0, Ritenuta=0, Totale=Imponibile

**Risultato:** Fatture conformi per professionisti in regime forfettario

---

## Esempio 3: Aggiungere Imposta di Bollo Digitale

**Richiesta utente:**
"Implementa l'imposta di bollo sulle fatture esenti IVA oltre 77,47€"

**Flusso di lavoro:**

1. **Analisi del dominio**
   - Leggi sezione 6 di `fiscal-rules.md` (Imposta di Bollo)
   - Regole chiave:
     - Obbligo: fatture esenti IVA con importo > 77,47€
     - Importo fisso: 2,00€
     - Modalità: bollo virtuale (DM 17/06/2014)

2. **Analisi del codice**
   - Creare nuovo servizio `BolloService`
   - Modello `Invoice`: aggiungere campo `decimal BolloAmount`
   - `InvoiceCalculationService`: chiamare `BolloService.Calculate()`

3. **Piano di implementazione**
   ```
   Regole fiscali:
   - DPR 642/1972, art. 13: Bollo su fatture esenti > 77,47€
   - DM 17/06/2014: Bollo virtuale

   Modifiche:
   1. IBolloService.cs - nuova interfaccia
   2. BolloService.cs - implementazione calcolo bollo
   3. Invoice.cs - aggiungi BolloAmount decimal
   4. InvoiceCalculationService.cs - integra BolloService
   5. Program.cs - registra BolloService in DI

   Test:
   1. Test_BolloRequired_WhenExemptAndOver77_47
   2. Test_BolloNotRequired_WhenExemptAndUnder77_47
   3. Test_BolloNotRequired_WhenVatApplied
   4. Test_BolloAmount_Is2Euros
   ```

4. **Implementazione**
   - Servizio separato per logica bollo (SRP)
   - Calcolo automatico basato su imponibile e aliquota IVA
   - Test verificano soglia e importo

**Risultato:** Conformità fiscale per fatture esenti IVA

---

## Esempio 4: Bug Fix NON Fiscale (quando NON usare la skill)

**Richiesta utente:**
"Correggi il bug: l'endpoint GET /api/invoices/{id} restituisce 500 invece di 404 se l'ID non esiste"

**Risposta corretta:**
❌ **NON usare questa skill**

**Perché:**
- Non ci sono regole fiscali coinvolte
- È un bug tecnico semplice nell'API
- Non impatta calcoli o validazioni di dominio

**Azione corretta:**
Implementa direttamente il fix nell'endpoint senza passare per il flusso di validazione fiscale.

---

## Quando NON Usare Questa Skill

❌ Bug fix puramente tecnici (errori HTTP, null reference, ecc.)
❌ Refactoring senza modifiche alla logica fiscale
❌ Miglioramenti UI/UX
❌ Ottimizzazioni performance
❌ Modifiche a infrastruttura (DB, logging, ecc.)

✅ Usare solo quando la feature tocca:
- Calcoli (IVA, ritenuta, bollo, totali)
- Validazioni fiscali (Partita IVA, transizioni di stato)
- Requisiti normativi (PA, split payment, forfettario)
- Modelli di dominio con impatto fiscale
