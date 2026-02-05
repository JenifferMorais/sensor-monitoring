using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Mappers;

public static class MapeadorSensor
{
    public static DtoSensor ParaDto(this Sensor sensor)
    {
        return new DtoSensor
        {
            Id = sensor.Id,
            Codigo = sensor.Codigo,
            Nome = sensor.Nome,
            Descricao = sensor.Descricao,
            EstaAtivo = sensor.EstaAtivo,
            CriadoEm = sensor.CriadoEm
        };
    }

    public static List<DtoSensor> ParaDto(this IEnumerable<Sensor> sensores)
    {
        return sensores.Select(ParaDto).ToList();
    }
}
