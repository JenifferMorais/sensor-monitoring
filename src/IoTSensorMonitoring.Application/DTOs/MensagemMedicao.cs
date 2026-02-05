namespace IoTSensorMonitoring.Application.DTOs;

public class MensagemMedicao
{
    public long IdMedicao { get; set; }
    public int IdSensor { get; set; }
    public string CodigoSensor { get; set; } = string.Empty;
    public decimal ValorMedicao { get; set; }
    public DateTimeOffset DataHoraMedicao { get; set; }
    public DateTimeOffset RecebidoEm { get; set; }
    public Guid? IdLote { get; set; }
}
