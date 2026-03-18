using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeterDataService.Data;

/// <summary>
/// Entita pro ukládání aplikaèních logù do SQLite databáze.
/// </summary>
[Table("Logs")]
public class AppLogEntry
{
    /// <summary>
    /// Primární klíè - automaticky generované ID.
    /// </summary>
    [Key]
    [Column("LogID")]
    public int LogId { get; set; }

    /// <summary>
    /// Text logovací zprávy.
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Datum a èas záznamu (UTC).
    /// </summary>
    public DateTime LogDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP adresa klienta (volitelné).
    /// </summary>
    [MaxLength(45)]
    public string? IP { get; set; }

    /// <summary>
    /// Identifikátor aplikace nebo komponenty.
    /// </summary>
    [MaxLength(100)]
    public string AppID { get; set; } = "MeterDataService";

    /// <summary>
    /// Závažnost logu: Information, Warning, Error.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Information";

    /// <summary>
    /// Typ logu - kategorie nebo zdroj (napø. TcpListener, SqliteProvider, CsvProvider).
    /// </summary>
    [MaxLength(100)]
    public string? Type { get; set; }
}