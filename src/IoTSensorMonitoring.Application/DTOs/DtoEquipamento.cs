namespace IoTSensorMonitoring.Application.DTOs;

public class DtoEquipamento
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool EstaAtivo { get; set; }
    public string NomeSetor { get; set; } = string.Empty;
    public int QuantidadeSensores { get; set; }
}
