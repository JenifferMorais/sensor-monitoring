using System.ComponentModel.DataAnnotations;

namespace IoTSensorMonitoring.Application.DTOs;

public class RequisicaoAtualizarSensor
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [MaxLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
    public string? Descricao { get; set; }

    public bool EstaAtivo { get; set; }
}
