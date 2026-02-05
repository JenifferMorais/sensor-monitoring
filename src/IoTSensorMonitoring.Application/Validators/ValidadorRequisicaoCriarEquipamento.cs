using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Validators;

public class ValidadorRequisicaoCriarEquipamento : AbstractValidator<RequisicaoCriarEquipamento>
{
    public ValidadorRequisicaoCriarEquipamento()
    {
        RuleFor(x => x.Nome)
            .NotEmpty()
            .WithMessage("O nome do equipamento é obrigatório")
            .MaximumLength(200)
            .WithMessage("O nome não pode ter mais de 200 caracteres");

        RuleFor(x => x.Descricao)
            .MaximumLength(500)
            .WithMessage("A descrição não pode ter mais de 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Descricao));

        RuleFor(x => x.IdSetor)
            .GreaterThan(0)
            .WithMessage("O ID do setor deve ser maior que 0");
    }
}
