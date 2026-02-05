using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Repositories;

public class RepositorioRegraAlerta : IRepositorioRegraAlerta
{
    private readonly ApplicationDbContext _context;

    public RepositorioRegraAlerta(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RegraAlerta>> ObterAtivasPorIdSensorAsync(int idSensor, CancellationToken tokenCancelamento = default)
    {
        return await _context.RegrasAlerta
            .Where(ra => ra.IdSensor == idSensor && ra.EstaAtivo)
            .AsNoTracking()
            .ToListAsync(tokenCancelamento);
    }
}
