using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Mappers;

public static class MapeadorMedicao
{
    public static DtoMedicaoCompleta ParaDtoCompleto(this Medicao medicao)
    {
        return new DtoMedicaoCompleta
        {
            Id = medicao.Id,
            IdSensor = medicao.IdSensor,
            CodigoSensor = medicao.Sensor?.Codigo ?? string.Empty,
            NomeSensor = medicao.Sensor?.Nome ?? string.Empty,
            ValorMedicao = medicao.ValorMedicao,
            DataHoraMedicao = medicao.DataHoraMedicao,
            RecebidoEm = medicao.RecebidoEm,
            IdLote = medicao.IdLote
        };
    }

    public static List<DtoMedicaoCompleta> ParaDtoCompleto(this IEnumerable<Medicao> medicoes)
    {
        return medicoes.Select(ParaDtoCompleto).ToList();
    }

    public static DtoMedicao ParaDto(this Medicao medicao)
    {
        return new DtoMedicao
        {
            DataHoraMedicao = medicao.DataHoraMedicao,
            Medicao = medicao.ValorMedicao,
            RecebidoEm = medicao.RecebidoEm
        };
    }

    public static List<DtoMedicao> ParaDto(this IEnumerable<Medicao> medicoes)
    {
        return medicoes.Select(ParaDto).ToList();
    }

    public static RespostaMedicoesEquipamento ParaRespostaMedicoesEquipamento(
        this Equipamento equipamento,
        Dictionary<int, List<Medicao>> gruposSensor)
    {
        var resposta = new RespostaMedicoesEquipamento
        {
            IdEquipamento = equipamento.Id,
            NomeEquipamento = equipamento.Nome,
            Sensores = new List<DtoMedicoesSensor>()
        };

        foreach (var vinculo in equipamento.VinculosSensorEquipamento.Where(l => l.EstaAtivo))
        {
            if (gruposSensor.TryGetValue(vinculo.IdSensor, out var medicoesSensor))
            {
                resposta.Sensores.Add(vinculo.ParaDtoMedicoesSensor(medicoesSensor));
            }
        }

        return resposta;
    }

    public static DtoMedicoesSensor ParaDtoMedicoesSensor(
        this VinculoSensorEquipamento vinculo,
        List<Medicao> medicoes)
    {
        return new DtoMedicoesSensor
        {
            IdSensor = vinculo.Sensor.Id,
            CodigoSensor = vinculo.Sensor.Codigo,
            UltimasMedicoes = medicoes.ParaDto()
        };
    }

    public static MensagemMedicao ParaMensagem(this Medicao medicao, Sensor sensor)
    {
        return new MensagemMedicao
        {
            IdMedicao = medicao.Id,
            IdSensor = sensor.Id,
            CodigoSensor = sensor.Codigo,
            ValorMedicao = medicao.ValorMedicao,
            DataHoraMedicao = medicao.DataHoraMedicao,
            RecebidoEm = medicao.RecebidoEm,
            IdLote = medicao.IdLote
        };
    }
}
