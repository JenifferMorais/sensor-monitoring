using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Domain.Interfaces;

public interface IRepositorioHistoricoAlerta
{
    Task AdicionarAsync(HistoricoAlerta historicoAlerta, CancellationToken tokenCancelamento = default);
    Task<List<HistoricoAlerta>> ObterAlertasEmailPendentesAsync(int limite, CancellationToken tokenCancelamento = default);
    Task AtualizarAsync(HistoricoAlerta historicoAlerta, CancellationToken tokenCancelamento = default);
}
