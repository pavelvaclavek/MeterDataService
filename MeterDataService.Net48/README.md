# MeterDataService.Net48

Port hlavní služby MeterDataService na .NET Framework 4.8 jako klasická Windows Service (`ServiceBase`).

## Struktura projektu

```
MeterDataService.Net48/
├── Program.cs                          # Vstupní bod (konzola / Windows Service)
├── MeterDataWindowsService.cs          # Hlavní třída služby (ServiceBase)
├── ProjectInstaller.cs                 # Instalátor pro installutil / sc.exe
├── App.config                          # Konfigurace (ekvivalent appsettings.json)
├── Models/
│   ├── MeterMessage.cs                 # Model příchozí zprávy z elektroměru
│   ├── DlmsMessage.cs                  # Model pro DLMS protokol
│   └── ServiceConfiguration.cs         # Konfigurační třídy + načítání z App.config
├── Workers/
│   └── DlmsMessageConverter.cs         # Konverze DLMS → MeterMessage
├── Providers/
│   ├── IDataProvider.cs                # Rozhraní pro datové providery
│   ├── DataProviderManager.cs          # Orchestrátor providerů (paralelní spuštění)
│   ├── CsvDataProvider.cs              # Ukládání do CSV souborů
│   ├── DatabaseDataProvider.cs         # Ukládání do SQL Serveru (EF6)
│   ├── SqliteDataProvider.cs           # Ukládání do SQLite (ADO.NET)
│   └── EmailDataProvider.cs            # Odesílání e-mailových notifikací
└── Data/
    ├── MeterDataContext.cs             # EF6 DbContext pro SQL Server
    └── MeterReading.cs                 # Entita pro SQL Server
```

## Rozdíly oproti .NET 8 verzi

| Oblast | .NET 8 | .NET 4.8 |
|---|---|---|
| Hosting | `BackgroundService` | `ServiceBase` + manuální `Task` |
| JSON | `System.Text.Json` | `Newtonsoft.Json` |
| DI kontejner | `Microsoft.Extensions.DependencyInjection` | Manuální konstrukce v `OnStart()` |
| Konfigurace | `appsettings.json` + `IOptions<T>` | `App.config` + `ConfigurationManager` |
| SQL Server | EF Core 9 + `IDbContextFactory` | Entity Framework 6 |
| SQLite | EF Core SQLite | `System.Data.SQLite` (raw ADO.NET) |
| Regex | `[GeneratedRegex]` (source gen) | `new Regex(..., RegexOptions.Compiled)` |
| Logování | `ILogger<T>` | `System.Diagnostics.EventLog` |

## Konfigurace (App.config)

Konfigurace se provádí v souboru `App.config` (po buildu `MeterDataService.Net48.exe.config`).

### Dostupné položky

| Klíč | Výchozí | Popis |
|---|---|---|
| `ListenPort` | `461` | TCP port pro naslouchání |
| `CsvOutputPath` | `C:\MeterData` | Adresář pro CSV soubory |
| `EnabledProviders` | `csv,sqlite` | Aktivní providery (čárkou oddělené): `csv`, `sqlite`, `database`, `email` |
| `Sqlite:DatabasePath` | `C:\MeterData\MeterData.db` | Cesta k SQLite souboru |
| `Email:SmtpServer` | — | Adresa SMTP serveru |
| `Email:SmtpPort` | `587` | Port SMTP serveru |
| `Email:Username` | — | Uživatelské jméno pro SMTP |
| `Email:Password` | — | Heslo pro SMTP |
| `Email:FromAddress` | — | Adresa odesílatele |
| `Email:ToAddresses` | — | Příjemci (čárkou oddělení) |
| `Email:UseSsl` | `true` | SSL/TLS pro SMTP |

SQL Server connection string je v sekci `<connectionStrings>` pod názvem `SqlServer`.

## Build

```bash
# Sestavení
dotnet build MeterDataService.Net48/MeterDataService.Net48.csproj

# Sestavení v Release módu
dotnet build MeterDataService.Net48/MeterDataService.Net48.csproj -c Release

# Publikování
dotnet publish MeterDataService.Net48/MeterDataService.Net48.csproj -c Release -o ./publish-net48
```

## Ladění (konzolový režim)

Při spuštění mimo Windows Service (např. z příkazové řádky nebo z Visual Studia) se aplikace automaticky spustí v konzolovém režimu:

```
MeterDataService.Net48.exe
```

Služba vypíše port, na kterém naslouchá, a čeká na stisknutí Enter pro ukončení. Detekce režimu probíhá přes `Environment.UserInteractive`.

## Instalace jako Windows Service

### Varianta 1: installutil

```bash
# Instalace (spustit jako Administrator)
installutil MeterDataService.Net48.exe

# Odinstalace
installutil /u MeterDataService.Net48.exe
```

### Varianta 2: sc.exe

```bash
# Instalace (spustit jako Administrator)
sc create MeterDataService binPath="C:\cesta\k\MeterDataService.Net48.exe" start=auto DisplayName="Meter Data Service (.NET 4.8)"
sc description MeterDataService "Sluzba pro prijem a zpracovani dat z elektrometru pres TCP"

# Spuštění
sc start MeterDataService

# Zastavení
sc stop MeterDataService

# Odinstalace
sc delete MeterDataService
```

### Varianta 3: PowerShell

```powershell
# Instalace (spustit jako Administrator)
New-Service -Name "MeterDataService" `
    -BinaryPathName "C:\cesta\k\MeterDataService.Net48.exe" `
    -DisplayName "Meter Data Service (.NET 4.8)" `
    -Description "Sluzba pro prijem a zpracovani dat z elektrometru pres TCP" `
    -StartupType Automatic

# Spuštění
Start-Service MeterDataService

# Zastavení
Stop-Service MeterDataService

# Odinstalace
Remove-Service MeterDataService
```

## Správa služby po instalaci

```bash
# Stav služby
sc query MeterDataService

# Zobrazení logů ve Windows Event Log
eventvwr.msc
# → Windows Logs → Application → filtrovat dle zdroje "MeterDataService"
```
