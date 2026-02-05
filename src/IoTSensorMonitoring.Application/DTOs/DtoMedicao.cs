namespace IoTSensorMonitoring.Application.DTOs;

public class DtoMedicaoCompleta
{
    public long Id { get; set; }
    public int IdSensor { get; set; }
    public string CodigoSensor { get; set; } = string.Empty;
    public string NomeSensor { get; set; } = string.Empty;
    public decimal ValorMedicao { get; set; }
    public DateTimeOffset DataHoraMedicao { get; set; }
    public DateTimeOffset RecebidoEm { get; set; }
    public Guid? IdLote { get; set; }
}
