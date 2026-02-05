using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTSensorMonitoring.Domain.Entities;

[Table("Setores")]
public class Setor
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

    [Column("EstaAtivo")]
    public bool EstaAtivo { get; set; } = true;

    public ICollection<Equipamento> Equipamentos { get; set; } = new List<Equipamento>();
}
