using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Domain.Interfaces;

public interface IRepositorioRegraAlerta
{
    Task<List<RegraAlerta>> ObterAtivasPorIdSensorAsync(int idSensor, CancellationToken tokenCancelamento = default);
}
