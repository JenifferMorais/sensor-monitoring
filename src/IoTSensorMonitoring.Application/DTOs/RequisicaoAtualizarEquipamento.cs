using System.ComponentModel.DataAnnotations;

namespace IoTSensorMonitoring.Application.DTOs;

public class RequisicaoAtualizarEquipamento
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [MaxLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
    public string? Descricao { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "O ID do setor deve ser maior que zero")]
    public int IdSetor { get; set; }

    public bool EstaAtivo { get; set; }
}
