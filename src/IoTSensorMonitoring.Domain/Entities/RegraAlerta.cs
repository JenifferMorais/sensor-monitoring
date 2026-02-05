using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTSensorMonitoring.Domain.Entities;

[Table("RegrasAlerta")]
public class RegraAlerta
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("IdSensor")]
    public int IdSensor { get; set; }

    [Column("TipoRegra")]
    public TipoRegraAlerta TipoRegra { get; set; }

    [Column("LimiteMinimo")]
    public decimal LimiteMinimo { get; set; }

    [Column("LimiteMaximo")]
    public decimal LimiteMaximo { get; set; }

    [Column("ContagemConsecutiva")]
    public int? ContagemConsecutiva { get; set; }

    [Column("TamanhoJanelaMedia")]
    public int? TamanhoJanelaMedia { get; set; }

    [Column("MargemErro")]
    public int? MargemErro { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("EmailNotificacao")]
    public string EmailNotificacao { get; set; } = string.Empty;

    [Column("EstaAtivo")]
    public bool EstaAtivo { get; set; } = true;

    [ForeignKey("IdSensor")]
    public Sensor Sensor { get; set; } = null!;
}

public enum TipoRegraAlerta
{
    ConsecutivoForaIntervalo = 1,
    MediaMargemErro = 2
}
