# Project Guidelines

## Overview

MeterDataService is a TCP-based Windows Service that receives JSON messages from electricity meters, parses them (standard + DLMS format), and routes data through pluggable providers (CSV, SQLite, SQL Server, Email) in parallel.

Two implementations exist side-by-side:
- **MeterDataService/** — .NET 8 Worker Service (EF Core 9, System.Text.Json, DI via Host builder)
- **MeterDataService.Net48/** — .NET Framework 4.8 port (EF6, Newtonsoft.Json, manual composition root, `ServiceBase`)

See [MeterDataService/DOKUMENTACE.md](../MeterDataService/DOKUMENTACE.md) for full architecture docs and message flow diagrams.

## Build and Test

```powershell
# Celá solution
dotnet build MeterDataService.sln

# Jednotlivé projekty
dotnet build MeterDataService/MeterDataService.csproj
dotnet build MeterDataService.Net48/MeterDataService.Net48.csproj
dotnet build MeterDataService.TestClient/MeterDataService.TestClient.csproj

# Spuštění služby lokálně (konzolový režim)
dotnet run --project MeterDataService/MeterDataService.csproj

# Testovací klient
dotnet run --project MeterDataService.TestClient/MeterDataService.TestClient.csproj
```

> ⚠ .NET 4.8 projekt vyžaduje Windows s .NET Framework 4.8 SDK / VS Build Tools. `msbuild` je robustnější alternativa k `dotnet build` pro tento projekt.

## Architecture

```
TCP klient → TcpListenerWorker → ExtractJsonMessages (brace-counting parser)
  → [DLMS? → DlmsMessageConverter] → MeterMessage
  → DataProviderManager → paralelně Task.WhenAll → IDataProvider[]
  → ACK response zpět klientovi
```

Key components:
- **Workers/TcpListenerWorker** — `BackgroundService` accepting TCP connections on `ListenPort`
- **Workers/DlmsMessageConverter** — Static converter: DLMS tabular data → `MeterMessage` via OBIS code regex
- **Providers/DataProviderManager** — Orchestrates active providers filtered by `EnabledProviders` config
- **Providers/IDataProvider** — Interface: `Name` + `ProcessAsync(MeterMessage, CancellationToken)`
- **Data/** — Dual EF contexts: `MeterDataContext` (SQL Server, migrations) + `SqliteMeterDataContext` (EnsureCreated)
- **Logging/** — `IAppLogger` with `SqliteAppLogger` / `NullAppLogger` (Null Object pattern)

## Conventions

- **Jazyk**: Dokumentace a komentáře jsou česky. Kód (identifikátory, API) je anglicky.
- **Provider pattern**: Nový data provider = implementace `IDataProvider`, registrace v DI (nebo manuální v .NET 4.8 `OnStart()`), přidání názvu do `EnabledProviders`.
- **Dual DB**: SQL Server uses `decimal(18,4)`, SQLite uses `double` (SQLite limitation). Always respect this in entity definitions.
- **Config binding**: .NET 8 → `IOptions<ServiceConfiguration>` from `appsettings.json` section `"ServiceConfiguration"`. .NET 4.8 → `ConfigurationManager.AppSettings` + custom `ServiceConfiguration` loader.
- **JSON framing**: TCP stream parsing counts `{` / `}` depth — no regex. Keep this pattern when modifying the listener.
- **Async all the way**: No blocking calls. `CancellationToken` propagated through the entire chain.
- **Concurrency**: `CsvDataProvider` uses `SemaphoreSlim` for file write safety. Follow same pattern for file-based providers.
- **Singleton registrations**: All providers and DataProviderManager are singletons in .NET 8 DI.

## Gotchas

- `context.Database.MigrateAsync()` in Program.cs is called without `await` — migrations run in background, potential race condition on first writes.
- EF Core migrations apply only to SQL Server context. SQLite uses `EnsureCreated()` (no migration history).
- DLMS detection differs: .NET 8 uses `System.Text.Json` (`JsonElement.ValueKind`), .NET 4.8 uses `Newtonsoft.Json` (`JToken.Type`) — edge-case parsing differences possible.
- Some source files have encoding issues in Czech diacritics comments (does not affect runtime).
- .NET 4.8 project uses C# 7.3 (`LangVersion`) — no pattern matching, nullable reference types, or modern syntax.

## Existing Documentation

- [MeterDataService/DOKUMENTACE.md](../MeterDataService/DOKUMENTACE.md) — Full architecture, message flow, DI setup, configuration reference
- [MeterDataService/README.md](../MeterDataService/README.md) — Project overview
- [MeterDataService.Net48/README.md](../MeterDataService.Net48/README.md) — .NET 4.8 variant, differences table, build/install instructions
