using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Repositories;

public class RepositorioHistoricoAlerta : IRepositorioHistoricoAlerta
{
    private readonly ApplicationDbContext _context;

    public RepositorioHistoricoAlerta(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(HistoricoAlerta historicoAlerta, CancellationToken tokenCancelamento = default)
    {
        await _context.HistoricoAlertas.AddAsync(historicoAlerta, tokenCancelamento);
        await _context.SaveChangesAsync(tokenCancelamento);
    }

    public async Task<List<HistoricoAlerta>> ObterAlertasEmailPendentesAsync(int limite, CancellationToken tokenCancelamento = default)
    {
        return await _context.HistoricoAlertas
            .Include(ha => ha.RegraAlerta)
            .Where(ha => !ha.EmailEnviado)
            .OrderBy(ha => ha.DisparadoEm)
            .Take(limite)
            .ToListAsync(tokenCancelamento);
    }

    public async Task AtualizarAsync(HistoricoAlerta historicoAlerta, CancellationToken tokenCancelamento = default)
    {
        _context.HistoricoAlertas.Update(historicoAlerta);
        await _context.SaveChangesAsync(tokenCancelamento);
    }
}
