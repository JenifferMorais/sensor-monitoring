namespace IoTSensorMonitoring.Application.DTOs;

public class RespostaMedicoesEquipamento
{
    public int IdEquipamento { get; set; }
    public string NomeEquipamento { get; set; } = string.Empty;
    public List<DtoMedicoesSensor> Sensores { get; set; } = new();
}

public class DtoMedicoesSensor
{
    public int IdSensor { get; set; }
    public string CodigoSensor { get; set; } = string.Empty;
    public List<DtoMedicao> UltimasMedicoes { get; set; } = new();
}

public class DtoMedicao
{
    public DateTimeOffset DataHoraMedicao { get; set; }
    public decimal Medicao { get; set; }
    public DateTimeOffset RecebidoEm { get; set; }
}
