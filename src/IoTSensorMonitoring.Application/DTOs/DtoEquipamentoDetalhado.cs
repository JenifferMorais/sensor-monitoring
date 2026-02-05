namespace IoTSensorMonitoring.Application.DTOs;

public class DtoEquipamentoDetalhado
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool EstaAtivo { get; set; }
    public DtoSetor? Setor { get; set; }
    public List<DtoSensorVinculado> Sensores { get; set; } = new();
}

public class DtoSetor
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
}

public class DtoSensorVinculado
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public bool EstaAtivo { get; set; }
    public DateTimeOffset VinculadoEm { get; set; }
    public string VinculadoPor { get; set; } = string.Empty;
}
