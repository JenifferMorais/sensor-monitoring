using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Domain.Interfaces;

public interface IRepositorioSensor
{
    Task<Sensor?> ObterPorIdAsync(int id, CancellationToken tokenCancelamento = default);
    Task<Sensor?> ObterPorCodigoAsync(string codigo, CancellationToken tokenCancelamento = default);
    Task<Sensor> AdicionarAsync(Sensor sensor, CancellationToken tokenCancelamento = default);
    Task AtualizarAsync(Sensor sensor, CancellationToken tokenCancelamento = default);
    Task<bool> DeletarAsync(int id, CancellationToken tokenCancelamento = default);
    Task<(List<Sensor> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina, CancellationToken tokenCancelamento = default);
}
