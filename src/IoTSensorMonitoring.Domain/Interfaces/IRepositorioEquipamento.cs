using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Domain.Interfaces;

public interface IRepositorioEquipamento
{
    Task<Equipamento?> ObterPorIdAsync(int id, CancellationToken tokenCancelamento = default);
    Task<Equipamento?> ObterPorIdComSensoresAsync(int id, CancellationToken tokenCancelamento = default);
    Task<(List<Equipamento> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina, CancellationToken tokenCancelamento = default);
    Task AtualizarAsync(Equipamento equipamento, CancellationToken tokenCancelamento = default);
    Task<bool> DeletarAsync(int id, CancellationToken tokenCancelamento = default);
}
