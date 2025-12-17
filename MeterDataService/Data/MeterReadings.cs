using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeterDataService.Data;

[Table("MeterReadings")]
public class MeterReading
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string SerialNumber { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    [MaxLength(50)]
    public string? MessageId { get; set; }

    [MaxLength(100)]
    public string? Network { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    [MaxLength(60)]
    public string? System { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Data_1_8_0 { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Data_1_8_1 { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Data_1_8_2 { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Data_2_8_0 { get; set; }

    public string? RawData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
