using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeterDataService.Data;

/// <summary>
/// Entita pro uloení namìøenıch dat z elektromìru do SQLite.
/// SQLite nepodporuje typ decimal, proto pouíváme REAL (double).
/// </summary>
[Table("MeterReadings")]
public class SqliteMeterReading
{
    /// <summary>
    /// Primární klíè - automaticky generované ID.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Sériové èíslo elektromìru.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Èasová znaèka mìøení (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// ID zprávy z elektromìru.
    /// </summary>
    [MaxLength(50)]
    public string? MessageId { get; set; }

    /// <summary>
    /// Sí, ze které data pøišla.
    /// </summary>
    [MaxLength(100)]
    public string? Network { get; set; }

    /// <summary>
    /// Model elektromìru.
    /// </summary>
    [MaxLength(100)]
    public string? Model { get; set; }

    /// <summary>
    /// Systém odesílající data.
    /// </summary>
    [MaxLength(60)]
    public string? System { get; set; }

    /// <summary>
    /// Registr 1.8.0 - Celková spotøeba (kWh).
    /// SQLite pouívá REAL místo decimal.
    /// </summary>
    public double? Data_1_8_0 { get; set; }

    /// <summary>
    /// Registr 1.8.1 - Spotøeba v tarifu 1 (kWh).
    /// </summary>
    public double? Data_1_8_1 { get; set; }

    /// <summary>
    /// Registr 1.8.2 - Spotøeba v tarifu 2 (kWh).
    /// </summary>
    public double? Data_1_8_2 { get; set; }

    /// <summary>
    /// Registr 2.8.0 - Celková dodávka do sítì (kWh).
    /// </summary>
    public double? Data_2_8_0 { get; set; }

    /// <summary>
    /// Surová data z elektromìru (pro diagnostiku).
    /// </summary>
    public string? RawData { get; set; }

    /// <summary>
    /// Datum a èas vytvoøení záznamu v databázi.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
