# MeterDataService — Dokumentace

## Přehled

**MeterDataService** je služba postavená na .NET 8 Worker Service, která běží jako Windows Service. Její hlavní účel je:

1. Naslouchat na TCP portu a přijímat JSON zprávy z elektroměrů
2. Rozparsovat každou zprávu a paralelně ji předat všem aktivním datovým providerům
3. Odeslat zpět klientovi JSON potvrzení (ACK) o zpracování

---

## Použité technologie

| Technologie | Verze | Účel |
|---|---|---|
| .NET 8 | 8.0 | Runtime a hosting |
| Entity Framework Core | 9.0.11 | ORM pro SQL Server a SQLite |
| EF Core SQLite | 9.0.11 | SQLite provider |
| EF Core SQL Server | 9.0.11 | SQL Server provider |
| Microsoft.Extensions.Hosting | 8.0.1 | Hosting a DI kontejner |
| Microsoft.Extensions.Hosting.WindowsServices | 8.0.1 | Podpora Windows Service |
| System.Text.Json | (součást .NET) | Serializace/deserializace JSON |
| System.Net.Sockets | (součást .NET) | TCP server/klient |

---

## Struktura projektu

```
MeterDataService.sln
│
├── MeterDataService/                    # Hlavní služba
│   ├── Program.cs                       # DI registrace a spuštění
│   ├── appsettings.json                 # Konfigurace
│   ├── appsettings.Development.json     # Vývojové přepisy
│   ├── Models/
│   │   ├── MeterMessage.cs              # Model příchozí zprávy z elektroměru
│   │   ├── DlmsMessage.cs               # Model pro DLMS protokol
│   │   └── ServiceConfiguration.cs      # Konfigurační třídy
│   ├── Workers/
│   │   ├── TcpListenerWorker.cs         # Hlavní BackgroundService — TCP server
│   │   └── DlmsMessageConverter.cs      # Konverze DLMS → MeterMessage
│   ├── Providers/
│   │   ├── IDataProvider.cs             # Rozhraní pro datové providery
│   │   ├── DataProviderManager.cs       # Orchestrátor providerů
│   │   ├── CsvDataProvider.cs           # Ukládání do CSV souborů
│   │   ├── DatabaseDataProvider.cs      # Ukládání do SQL Serveru
│   │   ├── SqliteDataProvider.cs        # Ukládání do SQLite
│   │   └── EmailDataProvider.cs         # Odesílání e-mailových notifikací
│   ├── Data/
│   │   ├── MeterDataContext.cs          # EF Core DbContext pro SQL Server
│   │   ├── MeterReadings.cs             # Entita pro SQL Server
│   │   ├── SqliteMeterDataContext.cs    # EF Core DbContext pro SQLite
│   │   ├── SqliteMeterReading.cs        # Entita pro SQLite
│   │   ├── MeterDataContextFactory.cs   # Design-time factory (SQL Server)
│   │   └── SqliteMeterDataContextFactory.cs  # Design-time factory (SQLite)
│   └── Migrations/                      # EF Core migrace pro SQL Server
│
└── MeterDataService.TestClient/         # Testovací klient
    └── Program.cs                       # Interaktivní/neinteraktivní TCP klient
```

---

## Tok zpráv (Message Flow)

```
TCP klient (elektroměr)
    │
    ▼
TcpListenerWorker (BackgroundService)
    │  Naslouchá na konfigurovatelném portu (výchozí 461)
    │  Přijímá TCP spojení
    ▼
ExtractJsonMessages()
    │  Parser založený na počítání hloubky složených závorek
    │  Bufferuje neúplné JSON zprávy
    ▼
Deserializace JSON → [Rozhodovací bod]
    │
    ├─ result.data je Array → cesta DLMS
    │   └─ DlmsMessageConverter.ToMeterMessages()
    │      Převod DLMS formátu na IEC 62056-21
    │
    └─ result.data je String → cesta MeterMessage
        └─ Přímá deserializace na MeterMessage
    │
    ▼
DataProviderManager.ProcessMessageAsync()
    │  Filtruje aktivní providery (case-insensitive)
    │  Spouští VŠECHNY aktivní providery paralelně (Task.WhenAll)
    ▼
Paralelní zpracování providery
    ├─ CsvDataProvider      → CSV soubory
    ├─ DatabaseDataProvider  → SQL Server
    ├─ SqliteDataProvider    → SQLite databáze
    └─ EmailDataProvider     → SMTP e-mail
    │
    ▼
ACK odpověď → zpět klientovi
    {"status":"ok", "sn":"...", "count":..., "timestamp":"..."}
```

---

## Klíčové třídy

### Modely

- **`MeterMessage`** — Hlavní datový model příchozí zprávy. Obsahuje sériové číslo elektroměru, Unix timestamp (jako string), síťová metadata a naměřená data ve formátu IEC 62056-21. Metoda `GetUtcDateTime()` převádí Unix epoch string na `DateTime`.
- **`DlmsMessage` / `DlmsResult` / `DlmsDataEntry`** — Modely pro DLMS protokol (tabulkový formát s OBIS kódy).
- **`ServiceConfiguration`** — Hlavní konfigurační třída vázaná z `appsettings.json`. Obsahuje vnořené třídy `EmailSettings`, `DatabaseSettings` a `SqliteSettings`.

### Workers

- **`TcpListenerWorker`** — Hlavní `BackgroundService`. Spouští TCP listener, přijímá klientská spojení, čte a parsuje JSON zprávy, předává je k zpracování a odesílá ACK odpovědi.
- **`DlmsMessageConverter`** — Statická třída pro konverzi DLMS formátu na `MeterMessage`. Parsuje OBIS kódy pomocí regulárních výrazů a mapuje je na standardní registry (1.8.0, 1.8.1, 1.8.2, 2.8.0).

### Providery

- **`IDataProvider`** — Rozhraní s vlastností `Name` a metodou `ProcessAsync(MeterMessage, CancellationToken) → bool`.
- **`DataProviderManager`** — Orchestrátor, který filtruje aktivní providery podle konfigurace a spouští je paralelně pomocí `Task.WhenAll`. Chyby v jednom provideru neblokují ostatní.
- **`CsvDataProvider`** — Zapisuje data do CSV souborů pojmenovaných `{SN}_{MM_yyyy}.csv`. Používá semafor pro bezpečný souběžný zápis.
- **`DatabaseDataProvider`** — Ukládá data do SQL Serveru přes EF Core. Používá `IDbContextFactory<MeterDataContext>`.
- **`SqliteDataProvider`** — Ukládá data do SQLite databáze. Používá `double` místo `decimal` (omezení SQLite).
- **`EmailDataProvider`** — Odesílá e-mailové notifikace s daty z elektroměru přes SMTP.

### Datová vrstva (EF Core)

- **`MeterDataContext`** — DbContext pro SQL Server. Používá EF Core migrace. Sloupce typu `decimal(18,4)`.
- **`SqliteMeterDataContext`** — DbContext pro SQLite. Inicializace přes `EnsureCreated()` (bez migrační historie). Sloupce typu `double`.
- Oba kontexty definují 3 indexy: na `SerialNumber`, `Timestamp` a kompozitní `SerialNumber + Timestamp`.

---

## Dependency Injection (Program.cs)

Registrace služeb probíhá v `Program.cs`:

1. **Konfigurace** — `ServiceConfiguration` je vázána z `appsettings.json` sekce `"ServiceConfiguration"`.
2. **DbContext factory** — Dvě `IDbContextFactory<T>` instance (SQL Server + SQLite), registrované jako singletony.
3. **Providery** — Všechny 4 implementace `IDataProvider` registrované jako singletony.
4. **Manager** — `DataProviderManager` jako singleton.
5. **Worker** — `TcpListenerWorker` jako hosted service.
6. **Windows Service** — `AddWindowsService()` s názvem `"MeterDataService"`.

Při startu se automaticky spustí:
- `MigrateAsync()` pro SQL Server (aplikuje migrace)
- `EnsureCreated()` pro SQLite (vytvoří schéma)

---

## Konfigurace — appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "EventLog": {
      "SourceName": "MeterDataService",
      "LogName": "Application",
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "ServiceConfiguration": {
    "ListenPort": 461,
    "CsvOutputPath": "C:\\MeterData",
    "EnabledProviders": ["csv", "sqlite"],

    "Database": {
      "ConnectionString": "Server=localhost;Database=MeterData;Trusted_Connection=True;TrustServerCertificate=True;"
    },

    "Sqlite": {
      "DatabasePath": "C:\\MeterData\\MeterData.db"
    },

    "Email": {
      "SmtpServer": "smtp.example.com",
      "SmtpPort": 587,
      "Username": "user@example.com",
      "Password": "password",
      "FromAddress": "meter-service@example.com",
      "ToAddresses": ["admin@example.com"],
      "UseSsl": true
    }
  }
}
```

### Popis konfiguračních položek

| Položka | Typ | Výchozí | Popis |
|---|---|---|---|
| `ListenPort` | int | 461 | TCP port, na kterém služba naslouchá |
| `CsvOutputPath` | string | `C:\MeterData` | Adresář pro ukládání CSV souborů |
| `EnabledProviders` | string[] | `["csv", "sqlite"]` | Seznam aktivních providerů (case-insensitive). Dostupné: `csv`, `sqlite`, `database`, `email` |
| `Database.ConnectionString` | string | — | Connection string pro SQL Server |
| `Sqlite.DatabasePath` | string | `C:\MeterData\MeterData.db` | Cesta k SQLite databázovému souboru |
| `Email.SmtpServer` | string | — | Adresa SMTP serveru |
| `Email.SmtpPort` | int | 587 | Port SMTP serveru |
| `Email.Username` | string | — | Uživatelské jméno pro SMTP autentizaci |
| `Email.Password` | string | — | Heslo pro SMTP (doporučeno uložit přes User Secrets) |
| `Email.FromAddress` | string | — | Adresa odesílatele e-mailu |
| `Email.ToAddresses` | string[] | — | Seznam příjemců e-mailu |
| `Email.UseSsl` | bool | true | Použití SSL/TLS pro SMTP |

### Citlivé hodnoty

Pro uložení hesel a connection stringů ve vývojovém prostředí se doporučuje použít .NET User Secrets:

```bash
dotnet user-secrets set "ServiceConfiguration:Database:ConnectionString" "Server=...;..."
dotnet user-secrets set "ServiceConfiguration:Email:Password" "tajne-heslo"
```

---

## Testovací klient (TestClient)

Interaktivní konzolová aplikace pro testování služby. Podporuje:

1. **Odeslání jedné testové zprávy** — s volitelným sériovým číslem
2. **Odeslání více zpráv s prodlevou** — zátěžový test
3. **Odeslání vlastního JSON** — libovolná data
4. **Test spojení** — ping
5. **Zátěžový test** — konfigurovatelný počet zpráv a souběžných spojení, měření latence
6. **Konfigurace** — nastavení hostitele, portu a timeoutu
7. **Ukončení**

Neinteraktivní režim:
```bash
dotnet run --project MeterDataService.TestClient -- <host> <port> [počet]
```

---

## Poznámky k implementaci

- **Parsování JSON** probíhá po znacích (sledování hloubky složených závorek `{}`), nikoliv přes regulární výrazy. To umožňuje spolehlivé zpracování streamovaných a neúplných zpráv.
- **DLMS podpora** — služba automaticky rozpozná formát DLMS (pole v `result.data`) a převede ho na standardní `MeterMessage`.
- **Paralelní zpracování** — všechny aktivní providery běží souběžně přes `Task.WhenAll`. Chyba v jednom provideru neovlivní ostatní.
- **Bezpečnost souborů** — `CsvDataProvider` používá `SemaphoreSlim` pro bezpečný souběžný zápis do CSV souborů.
- **Dva DbContexty** — SQL Server používá migrace (`MigrateAsync`), SQLite používá `EnsureCreated()` bez migrační historie.
- **Plná asynchronní architektura** — žádné blokující volání, podpora `CancellationToken` v celém řetězci.

---

## Příkazy pro sestavení a spuštění

```bash
# Sestavení řešení
dotnet build MeterDataService.sln

# Spuštění služby
dotnet run --project MeterDataService/MeterDataService.csproj

# Spuštění testovacího klienta (interaktivně)
dotnet run --project MeterDataService.TestClient/MeterDataService.TestClient.csproj

# Spuštění testovacího klienta neinteraktivně
dotnet run --project MeterDataService.TestClient/MeterDataService.TestClient.csproj -- 127.0.0.1 461 5

# Publikování jako Windows Service
dotnet publish MeterDataService/MeterDataService.csproj -c Release -o ./publish

# EF Core migrace (spouštět z adresáře MeterDataService/)
dotnet ef migrations add <NazevMigrace> --context MeterDataContext
dotnet ef database update --context MeterDataContext
```
