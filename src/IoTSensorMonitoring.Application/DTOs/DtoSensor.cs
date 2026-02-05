namespace IoTSensorMonitoring.Application.DTOs;

public class DtoSensor
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool EstaAtivo { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
