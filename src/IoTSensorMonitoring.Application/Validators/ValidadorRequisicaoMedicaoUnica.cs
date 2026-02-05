using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Validators;

public class ValidadorRequisicaoMedicaoUnica : AbstractValidator<RequisicaoMedicaoUnica>
{
    public ValidadorRequisicaoMedicaoUnica()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("O ID do sensor deve ser maior que 0");

        RuleFor(x => x.Codigo)
            .NotEmpty()
            .WithMessage("O código do sensor é obrigatório")
            .MaximumLength(50)
            .WithMessage("O código não pode ter mais de 50 caracteres");

        RuleFor(x => x.DataHoraMedicao)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("A data/hora da medição não pode ser no futuro")
            .GreaterThan(DateTimeOffset.UtcNow.AddYears(-1))
            .WithMessage("A data/hora da medição não pode ser anterior a 1 ano");

        RuleFor(x => x.Medicao)
            .GreaterThanOrEqualTo(-273.15m)
            .WithMessage("O valor da medição deve ser maior ou igual a -273.15 (zero absoluto)")
            .LessThanOrEqualTo(1000m)
            .WithMessage("O valor da medição não pode exceder 1000");
    }
}
