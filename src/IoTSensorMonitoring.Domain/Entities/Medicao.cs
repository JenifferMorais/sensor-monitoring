using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTSensorMonitoring.Domain.Entities;

[Table("Medicoes")]
public class Medicao
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Column("IdSensor")]
    public int IdSensor { get; set; }

    [Column("DataHoraMedicao")]
    public DateTimeOffset DataHoraMedicao { get; set; }

    [Column("ValorMedicao")]
    public decimal ValorMedicao { get; set; }

    [Column("RecebidoEm")]
    public DateTimeOffset RecebidoEm { get; set; } = DateTimeOffset.UtcNow;

    [Column("IdLote")]
    public Guid? IdLote { get; set; }

    [ForeignKey("IdSensor")]
    public Sensor Sensor { get; set; } = null!;
}
