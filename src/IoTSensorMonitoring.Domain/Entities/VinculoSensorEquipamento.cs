using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTSensorMonitoring.Domain.Entities;

[Table("VinculosSensorEquipamento")]
public class VinculoSensorEquipamento
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("IdSensor")]
    public int IdSensor { get; set; }

    [Column("IdEquipamento")]
    public int IdEquipamento { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("VinculadoPor")]
    public string VinculadoPor { get; set; } = string.Empty;

    [Column("VinculadoEm")]
    public DateTimeOffset VinculadoEm { get; set; } = DateTimeOffset.UtcNow;

    [Column("EstaAtivo")]
    public bool EstaAtivo { get; set; } = true;

    [ForeignKey("IdSensor")]
    public Sensor Sensor { get; set; } = null!;

    [ForeignKey("IdEquipamento")]
    public Equipamento Equipamento { get; set; } = null!;
}
