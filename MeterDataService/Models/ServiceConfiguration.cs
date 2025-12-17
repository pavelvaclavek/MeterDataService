namespace MeterDataService.Models
{
    public class ServiceConfiguration
    {
        public int ListenPort { get; set; } = 461;
        public string CsvOutputPath { get; set; } = "C:\\MeterData";
        public List<string> EnabledProviders { get; set; } = new() { "csv" };
        public EmailSettings Email { get; set; } = new();
        public DatabaseSettings Database { get; set; } = new();
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public List<string> ToAddresses { get; set; } = new();
        public bool UseSsl { get; set; } = true;
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
    }
}