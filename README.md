# Fatturazione API

API REST per la gestione della fatturazione italiana, costruita con ASP.NET Core 8 Minimal APIs.

## Descrizione

Fatturazione è un sistema di fatturazione elettronica italiana che permette di gestire clienti e fatture, con supporto completo per:

- **Gestione clienti** — Professionisti, Aziende e Pubblica Amministrazione
- **Gestione fatture** — Creazione, modifica, emissione e cancellazione
- **Calcolo automatico dei totali** — Imponibile, IVA, ritenuta d'acconto e netto a pagare
- **Aliquote IVA italiane** — 22% (ordinaria), 10% (ridotta), 4% (super-ridotta), 0% (esente)
- **Ritenuta d'acconto** — Calcolo automatico per i professionisti (default 20%)
- **Validazione Partita IVA** — Controllo formale con algoritmo di checksum italiano
- **Numerazione progressiva fatture** — Formato `YYYY/NNN` (es. `2026/001`)

## Prerequisiti

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (o superiore)

Per verificare l'installazione:

```bash
dotnet --version
```

## Avvio rapido

### Con lo script `run.sh` (macOS/Linux)

```bash
chmod +x run.sh
./run.sh
```

Lo script compila il progetto, avvia l'API e apre automaticamente il browser su Swagger UI.

### Manualmente

```bash
# Compilazione
dotnet build

# Avvio
dotnet run --project src/Fatturazione.Api
```

L'applicazione sarà disponibile su:

| URL | Descrizione |
|-----|-------------|
| http://localhost:5298 | Redirect a Swagger UI |
| http://localhost:5298/swagger | Swagger UI (documentazione interattiva) |
| http://localhost:5298/api/clients | Endpoint clienti |
| http://localhost:5298/api/invoices | Endpoint fatture |

## Architettura

Il progetto segue un'architettura a layer:

```
Fatturazione.sln
└── src/
    ├── Fatturazione.Api              # Minimal APIs, endpoint REST, configurazione
    ├── Fatturazione.Domain           # Modelli, servizi di dominio, validatori
    └── Fatturazione.Infrastructure   # Data store in-memory, repository
```

### Fatturazione.Domain

Contiene la logica di business pura, senza dipendenze esterne:

- **Models** — `Client`, `Invoice`, `InvoiceItem`, `Address`, enums (`ClientType`, `InvoiceStatus`, `IvaRate`)
- **Services** — Calcolo totali fattura, ritenuta d'acconto, numerazione progressiva
- **Validators** — Validazione Partita IVA (checksum), validazione fattura, transizioni di stato

### Fatturazione.Infrastructure

- **InMemoryDataStore** — Store thread-safe basato su `ConcurrentDictionary`
- **Repository** — Pattern repository per `Client` e `Invoice`

### Fatturazione.Api

- **Endpoints** — Minimal API organizzati per risorsa (`/api/clients`, `/api/invoices`)
- **Swagger/OpenAPI** — Documentazione automatica dell'API
- **Seed data** — Dati di esempio caricati all'avvio (4 clienti, 3 fatture)

## API Endpoints

### Clienti (`/api/clients`)

| Metodo | Path | Descrizione |
|--------|------|-------------|
| `GET` | `/api/clients` | Lista tutti i clienti |
| `GET` | `/api/clients/{id}` | Dettaglio cliente per ID |
| `POST` | `/api/clients` | Crea un nuovo cliente |
| `PUT` | `/api/clients/{id}` | Aggiorna un cliente |
| `DELETE` | `/api/clients/{id}` | Elimina un cliente |
| `GET` | `/api/clients/validate-partita-iva/{partitaIva}` | Valida una Partita IVA |

### Fatture (`/api/invoices`)

| Metodo | Path | Descrizione |
|--------|------|-------------|
| `GET` | `/api/invoices` | Lista tutte le fatture |
| `GET` | `/api/invoices/{id}` | Dettaglio fattura per ID |
| `GET` | `/api/invoices/by-client/{clientId}` | Fatture di un cliente |
| `POST` | `/api/invoices` | Crea una nuova fattura |
| `PUT` | `/api/invoices/{id}` | Aggiorna una fattura |
| `POST` | `/api/invoices/{id}/calculate` | Ricalcola i totali |
| `POST` | `/api/invoices/{id}/issue` | Emetti fattura (assegna numero e stato "Issued") |
| `DELETE` | `/api/invoices/{id}` | Elimina una fattura |

## Dati di esempio

All'avvio vengono creati automaticamente:

**Clienti:**
- Studio Rossi & Associati (Professionista, soggetto a ritenuta)
- TechSolutions SRL (Azienda)
- Freelance Designer - Anna Bianchi (Professionista, soggetta a ritenuta)
- Comune di Firenze (Pubblica Amministrazione)

**Fatture:**
- `2026/001` — Consulenza legale (stato: Issued, con ritenuta)
- `2026/002` — Sviluppo software (stato: Sent, senza ritenuta)
- Bozza — Design branding (stato: Draft)

## Note

- Lo storage dei dati è **in-memory**: tutti i dati vengono persi al riavvio dell'applicazione
- CORS è abilitato per qualsiasi origine (configurazione demo)
- Il progetto non include autenticazione/autorizzazione
