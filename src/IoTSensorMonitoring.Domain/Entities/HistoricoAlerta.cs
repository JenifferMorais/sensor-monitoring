using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTSensorMonitoring.Domain.Entities;

[Table("HistoricoAlertas")]
public class HistoricoAlerta
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Column("IdSensor")]
    public int IdSensor { get; set; }

    [Column("IdRegraAlerta")]
    public int IdRegraAlerta { get; set; }

    [Column("TipoRegra")]
    public TipoRegraAlerta TipoRegra { get; set; }

    [Required]
    [MaxLength(500)]
    [Column("MotivoDisparo")]
    public string MotivoDisparo { get; set; } = string.Empty;

    [Column("ValorDisparo")]
    public decimal? ValorDisparo { get; set; }

    [Column("DisparadoEm")]
    public DateTimeOffset DisparadoEm { get; set; } = DateTimeOffset.UtcNow;

    [Column("EmailEnviado")]
    public bool EmailEnviado { get; set; }

    [Column("EmailEnviadoEm")]
    public DateTimeOffset? EmailEnviadoEm { get; set; }

    [ForeignKey("IdSensor")]
    public Sensor Sensor { get; set; } = null!;

    [ForeignKey("IdRegraAlerta")]
    public RegraAlerta RegraAlerta { get; set; } = null!;
}
