using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Domain.Interfaces;

public interface IRepositorioVinculoSensorEquipamento
{
    Task<VinculoSensorEquipamento?> ObterVinculoAtivoAsync(int idSensor, int idEquipamento, CancellationToken tokenCancelamento = default);
    Task<VinculoSensorEquipamento> AdicionarAsync(VinculoSensorEquipamento vinculo, CancellationToken tokenCancelamento = default);
}
