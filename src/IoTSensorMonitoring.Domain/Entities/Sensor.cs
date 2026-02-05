using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTSensorMonitoring.Domain.Entities;

[Table("Sensores")]
public class Sensor
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("Codigo")]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("Nome")]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("Descricao")]
    public string? Descricao { get; set; }

    [Column("EstaAtivo")]
    public bool EstaAtivo { get; set; } = true;

    [Column("CriadoEm")]
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Medicao> Medicoes { get; set; } = new List<Medicao>();
    public ICollection<VinculoSensorEquipamento> VinculosSensorEquipamento { get; set; } = new List<VinculoSensorEquipamento>();
    public ICollection<RegraAlerta> RegrasAlerta { get; set; } = new List<RegraAlerta>();
}
