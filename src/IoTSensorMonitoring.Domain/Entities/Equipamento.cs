using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTSensorMonitoring.Domain.Entities;

[Table("Equipamentos")]
public class Equipamento
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("Nome")]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("Descricao")]
    public string? Descricao { get; set; }

    [Column("IdSetor")]
    public int IdSetor { get; set; }

    [Column("EstaAtivo")]
    public bool EstaAtivo { get; set; } = true;

    [ForeignKey("IdSetor")]
    public Setor Setor { get; set; } = null!;

    public ICollection<VinculoSensorEquipamento> VinculosSensorEquipamento { get; set; } = new List<VinculoSensorEquipamento>();
}
