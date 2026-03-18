using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace MeterDataService.Net48.Models
{
    public class ServiceConfiguration
    {
        public int ListenPort { get; set; } = 461;
        public string CsvOutputPath { get; set; } = "C:\\MeterData";
        public List<string> EnabledProviders { get; set; } = new List<string> { "csv" };
        public EmailSettings Email { get; set; } = new EmailSettings();
        public DatabaseSettings Database { get; set; } = new DatabaseSettings();
        public SqliteSettings Sqlite { get; set; } = new SqliteSettings();
        public LoggingSettings AppLogging { get; set; } = new LoggingSettings();

        public static ServiceConfiguration Load()
        {
            var config = new ServiceConfiguration();

            var listenPort = ConfigurationManager.AppSettings["ListenPort"];
            if (int.TryParse(listenPort, out var port))
                config.ListenPort = port;

            var csvPath = ConfigurationManager.AppSettings["CsvOutputPath"];
            if (!string.IsNullOrEmpty(csvPath))
                config.CsvOutputPath = csvPath;

            var providers = ConfigurationManager.AppSettings["EnabledProviders"];
            if (!string.IsNullOrEmpty(providers))
                config.EnabledProviders = providers.Split(',').Select(p => p.Trim()).ToList();

            // SQLite
            var sqlitePath = ConfigurationManager.AppSettings["Sqlite:DatabasePath"];
            if (!string.IsNullOrEmpty(sqlitePath))
                config.Sqlite.DatabasePath = sqlitePath;

            // Database (SQL Server)
            var connStr = ConfigurationManager.ConnectionStrings["SqlServer"];
            if (connStr != null)
                config.Database.ConnectionString = connStr.ConnectionString;

            // Email
            var smtpServer = ConfigurationManager.AppSettings["Email:SmtpServer"];
            if (!string.IsNullOrEmpty(smtpServer))
                config.Email.SmtpServer = smtpServer;

            var smtpPort = ConfigurationManager.AppSettings["Email:SmtpPort"];
            if (int.TryParse(smtpPort, out var emailPort))
                config.Email.SmtpPort = emailPort;

            var username = ConfigurationManager.AppSettings["Email:Username"];
            if (!string.IsNullOrEmpty(username))
                config.Email.Username = username;

            var password = ConfigurationManager.AppSettings["Email:Password"];
            if (!string.IsNullOrEmpty(password))
                config.Email.Password = password;

            var fromAddress = ConfigurationManager.AppSettings["Email:FromAddress"];
            if (!string.IsNullOrEmpty(fromAddress))
                config.Email.FromAddress = fromAddress;

            var toAddresses = ConfigurationManager.AppSettings["Email:ToAddresses"];
            if (!string.IsNullOrEmpty(toAddresses))
                config.Email.ToAddresses = toAddresses.Split(',').Select(a => a.Trim()).ToList();

            var useSsl = ConfigurationManager.AppSettings["Email:UseSsl"];
            if (bool.TryParse(useSsl, out var ssl))
                config.Email.UseSsl = ssl;

            // App Logging
            var logger = ConfigurationManager.AppSettings["AppLogging:Logger"];
            if (!string.IsNullOrEmpty(logger))
                config.AppLogging.Logger = logger;

            var loggingEnabled = ConfigurationManager.AppSettings["AppLogging:Enabled"];
            if (bool.TryParse(loggingEnabled, out var enabled))
                config.AppLogging.Enabled = enabled;

            var includeJson = ConfigurationManager.AppSettings["AppLogging:IncludeJsonInMessage"];
            if (bool.TryParse(includeJson, out var includeJsonValue))
                config.AppLogging.IncludeJsonInMessage = includeJsonValue;

            return config;
        }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public List<string> ToAddresses { get; set; } = new List<string>();
        public bool UseSsl { get; set; } = true;
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class SqliteSettings
    {
        public string DatabasePath { get; set; } = "MeterData.db";
    }

    /// <summary>
    /// Nastavení pro aplikaèní logování do databáze.
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// Typ loggeru: "sqlite" nebo "none".
        /// </summary>
        public string Logger { get; set; } = "sqlite";

        /// <summary>
        /// Povolit logování do databáze.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Zahrnout JSON zprávu do textu logované Message.
        /// Pokud false, do Message se uloží pouze informace o chybì bez obsahu JSON.
        /// </summary>
        public bool IncludeJsonInMessage { get; set; } = false;
    }
}
