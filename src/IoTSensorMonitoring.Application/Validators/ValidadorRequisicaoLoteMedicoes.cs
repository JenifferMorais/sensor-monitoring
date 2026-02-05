using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Validators;

public class ValidadorRequisicaoLoteMedicoes : AbstractValidator<RequisicaoLoteMedicoes>
{
    public ValidadorRequisicaoLoteMedicoes()
    {
        RuleFor(x => x.Medicoes)
            .NotEmpty()
            .WithMessage("A lista de medições não pode estar vazia")
            .Must(x => x.Count <= 10000)
            .WithMessage("O lote não pode conter mais de 10.000 medições");

        RuleForEach(x => x.Medicoes)
            .SetValidator(new ValidadorRequisicaoMedicaoUnica());
    }
}
