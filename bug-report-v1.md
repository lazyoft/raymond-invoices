# 🐛 Bug Report - Fatturazione API

**Data analisi**: 14 Febbraio 2026
**Branch**: Bugs
**Stato test**: ✅ 140 test passati (114 Domain + 26 API)

---

## 1. 🔴 **BUG CRITICO: Calcolo Errato della Ritenuta d'Acconto**

### Descrizione
La ritenuta d'acconto viene calcolata **sul SubTotal (Imponibile + IVA)** invece che **sul solo Imponibile**, violando la normativa fiscale italiana.

### File coinvolto
`src/Fatturazione.Domain/Services/InvoiceCalculationService.cs:82-93`

### Codice problematico
```csharp
public decimal CalculateRitenutaAmount(Invoice invoice)
{
    if (invoice.Client == null || !_ritenutaService.AppliesRitenuta(invoice.Client))
    {
        return 0;
    }

    return _ritenutaService.CalculateRitenuta(
        invoice.SubTotal,  // ❌ BUG: dovrebbe essere invoice.ImponibileTotal
        invoice.Client.RitenutaPercentage
    );
}
```

### Esempio del problema
**Input:**
- Imponibile: €1.000,00
- IVA 22%: €220,00
- SubTotal: €1.220,00
- Ritenuta: 20%

**Comportamento attuale (ERRATO):**
- Ritenuta calcolata su: €1.220,00
- Ritenuta: €244,00
- **Totale da pagare: €976,00** ❌

**Comportamento corretto:**
- Ritenuta calcolata su: €1.000,00
- Ritenuta: €200,00
- **Totale da pagare: €1.020,00** ✅

### Impatto
- ⚠️ **ALTO** - Errore di calcolo fiscale che produce fatture con importi errati
- 💰 Differenza economica significativa (nel caso sopra: €44 di errore)
- 📋 Non conforme alla normativa italiana sulla ritenuta d'acconto

### Test documentati
Il bug è **già documentato** nei test:
- `InvoiceCalculationServiceTests.cs:296` - `CalculateInvoiceTotals_WithClientSubjectToRitenuta_CalculatesOnSubTotal` (Trait: "KnownBug")
- `InvoiceCalculationServiceTests.cs:337` - `CalculateRitenutaAmount_PassesSubTotalToService_NotImponibile` (Trait: "KnownBug")

### Fix necessario
```csharp
return _ritenutaService.CalculateRitenuta(
    invoice.ImponibileTotal,  // ✅ Corretto
    invoice.Client.RitenutaPercentage
);
```

---

## 2. 🟡 **VALIDAZIONE MANCANTE: Quantità e Prezzi Negativi/Zero**

### Descrizione
Il `InvoiceValidator` non verifica che `Quantity` e `UnitPrice` degli `InvoiceItem` siano valori positivi.

### File coinvolto
`src/Fatturazione.Domain/Validators/InvoiceValidator.cs:55-65`

### Codice problematico
```csharp
private static List<string> ValidateInvoiceItem(InvoiceItem item, int itemNumber)
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(item.Description))
    {
        errors.Add($"Item {itemNumber}: Description è obbligatoria");
    }
    // ❌ MANCA: validazione Quantity > 0
    // ❌ MANCA: validazione UnitPrice >= 0

    return errors;
}
```

### Scenario problematico
È possibile creare fatture con:
- `Quantity = 0` → totale sempre zero
- `Quantity < 0` → quantità negative (nonsense)
- `UnitPrice = 0` → prezzo zero (potrebbe essere legittimo in alcuni casi)
- `UnitPrice < 0` → prezzo negativo (nonsense)

### Impatto
- ⚠️ **MEDIO** - Dati inconsistenti nel sistema
- 🧮 Possibili calcoli errati o fatture con totali a zero
- 🔍 Difficoltà nel debugging di fatture anomale

### Fix suggerito
```csharp
private static List<string> ValidateInvoiceItem(InvoiceItem item, int itemNumber)
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(item.Description))
    {
        errors.Add($"Item {itemNumber}: Description è obbligatoria");
    }

    if (item.Quantity <= 0)
    {
        errors.Add($"Item {itemNumber}: Quantity deve essere maggiore di zero");
    }

    if (item.UnitPrice < 0)
    {
        errors.Add($"Item {itemNumber}: UnitPrice non può essere negativo");
    }

    return errors;
}
```

---

## 3. ℹ️ **NOTA: Numerazione Fatture Cross-Year**

### Osservazione
La numerazione delle fatture **NON si resetta** al cambio anno. Questo è **intenzionale e corretto** secondo la normativa italiana dal 2013.

### File
`src/Fatturazione.Domain/Services/InvoiceNumberingService.cs:33-35`

### Comportamento attuale
```csharp
int nextSequence = lastSequence + 1;
return $"{currentYear}/{nextSequence:D3}";
```

**Esempio:**
- Ultima fattura 2025: `2025/005`
- Prima fattura 2026: `2026/006` ✅ (non `2026/001`)

### Test di verifica
`InvoiceNumberingServiceTests.cs:59-71` - `GenerateNextInvoiceNumber_WithPreviousYear_ContinuesSequence`

Commento nel test:
```csharp
// Note: Dal 2013, la numerazione progressiva può continuare senza reset annuale
```

**Conclusione**: ✅ Non è un bug, è comportamento corretto.

---

## 📊 Riepilogo

| # | Tipo | Severità | Descrizione | File |
|---|------|----------|-------------|------|
| 1 | 🐛 Bug | 🔴 Critico | Ritenuta calcolata su SubTotal invece di Imponibile | `InvoiceCalculationService.cs:89` |
| 2 | ⚠️ Validazione | 🟡 Media | Manca validazione Quantity/UnitPrice > 0 | `InvoiceValidator.cs:55-65` |
| 3 | ℹ️ Nota | - | Numerazione cross-year è intenzionale | `InvoiceNumberingService.cs:33` |

---

## 🎯 Raccomandazioni

### Priorità 1 (Urgente)
- ✅ Correggere il calcolo della ritenuta (Bug #1)
- ✅ Aggiornare i test da "KnownBug" a test normali

### Priorità 2 (Alta)
- ✅ Aggiungere validazione su Quantity e UnitPrice (Bug #2)
- ✅ Aggiungere test per scenari con valori zero/negativi

### Priorità 3 (Opzionale)
- 📝 Documentare meglio il comportamento della numerazione cross-year nel README

---

**Test Coverage**: Il bug #1 è già documentato nei test con trait `KnownBug`. Tutti i test attuali passano (140/140), ma documentano il comportamento errato come "atteso".
