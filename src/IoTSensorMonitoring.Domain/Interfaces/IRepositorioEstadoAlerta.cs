using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Domain.Interfaces;

public interface IRepositorioEstadoAlerta
{
    Task<EstadoAlerta?> ObterPorRegraAlertaAsync(int idSensor, int idRegraAlerta, CancellationToken tokenCancelamento = default);
    Task InserirOuAtualizarAsync(EstadoAlerta estado, CancellationToken tokenCancelamento = default);
}
