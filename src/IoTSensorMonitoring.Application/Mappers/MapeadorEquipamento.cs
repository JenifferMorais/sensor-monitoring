using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Mappers;

public static class MapeadorEquipamento
{
    public static DtoEquipamento ParaDto(this Equipamento equipamento)
    {
        return new DtoEquipamento
        {
            Id = equipamento.Id,
            Nome = equipamento.Nome,
            Descricao = equipamento.Descricao,
            EstaAtivo = equipamento.EstaAtivo,
            NomeSetor = equipamento.Setor?.Nome ?? "Sem setor",
            QuantidadeSensores = equipamento.VinculosSensorEquipamento.Count
        };
    }

    public static List<DtoEquipamento> ParaDto(this IEnumerable<Equipamento> equipamentos)
    {
        return equipamentos.Select(ParaDto).ToList();
    }

    public static DtoEquipamentoDetalhado ParaDtoDetalhado(this Equipamento equipamento)
    {
        return new DtoEquipamentoDetalhado
        {
            Id = equipamento.Id,
            Nome = equipamento.Nome,
            Descricao = equipamento.Descricao,
            EstaAtivo = equipamento.EstaAtivo,
            Setor = equipamento.Setor != null ? new DtoSetor
            {
                Id = equipamento.Setor.Id,
                Nome = equipamento.Setor.Nome
            } : null,
            Sensores = equipamento.VinculosSensorEquipamento
                .Select(vse => new DtoSensorVinculado
                {
                    Id = vse.Sensor.Id,
                    Codigo = vse.Sensor.Codigo,
                    Nome = vse.Sensor.Nome,
                    EstaAtivo = vse.Sensor.EstaAtivo,
                    VinculadoEm = vse.VinculadoEm,
                    VinculadoPor = vse.VinculadoPor
                })
                .ToList()
        };
    }
}
