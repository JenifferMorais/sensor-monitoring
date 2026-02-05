using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTSensorMonitoring.Domain.Entities;

[Table("EstadosAlerta")]
public class EstadoAlerta
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("IdSensor")]
    public int IdSensor { get; set; }

    [Column("IdRegraAlerta")]
    public int IdRegraAlerta { get; set; }

    [Column("ContagemConsecutiva")]
    public int ContagemConsecutiva { get; set; }

    [Required]
    [Column("JsonMedicoesRecentes")]
    public string JsonMedicoesRecentes { get; set; } = "[]";

    [Column("UltimaAtualizacao")]
    public DateTimeOffset UltimaAtualizacao { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey("IdSensor")]
    public Sensor Sensor { get; set; } = null!;

    [ForeignKey("IdRegraAlerta")]
    public RegraAlerta RegraAlerta { get; set; } = null!;
}
