using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Domain.Interfaces;

public interface IRepositorioMedicao
{
    Task<Medicao?> ObterPorIdAsync(int id, CancellationToken tokenCancelamento = default);
    Task<Medicao> AdicionarAsync(Medicao medicao, CancellationToken tokenCancelamento = default);
    Task AdicionarLoteAsync(IEnumerable<Medicao> medicoes, CancellationToken tokenCancelamento = default);
    Task<List<Medicao>> ObterUltimasPorEquipamentoAsync(int idEquipamento, int limitePorSensor = 10, CancellationToken tokenCancelamento = default);
    Task<List<Medicao>> ObterRecentesPorSensorAsync(int idSensor, int quantidade, CancellationToken tokenCancelamento = default);
    Task<(List<Medicao> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina, int? idSensor = null, CancellationToken tokenCancelamento = default);
}
