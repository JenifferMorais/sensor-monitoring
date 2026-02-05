using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Repositories;

public class RepositorioSensor : IRepositorioSensor
{
    private readonly ApplicationDbContext _context;

    public RepositorioSensor(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Sensor?> ObterPorIdAsync(int id, CancellationToken tokenCancelamento = default)
    {
        return await _context.Sensores
            .FirstOrDefaultAsync(s => s.Id == id, tokenCancelamento);
    }

    public async Task<Sensor?> ObterPorCodigoAsync(string codigo, CancellationToken tokenCancelamento = default)
    {
        return await _context.Sensores
            .FirstOrDefaultAsync(s => s.Codigo == codigo, tokenCancelamento);
    }

    public async Task<Sensor> AdicionarAsync(Sensor sensor, CancellationToken tokenCancelamento = default)
    {
        await _context.Sensores.AddAsync(sensor, tokenCancelamento);
        await _context.SaveChangesAsync(tokenCancelamento);
        return sensor;
    }

    public async Task AtualizarAsync(Sensor sensor, CancellationToken tokenCancelamento = default)
    {
        _context.Sensores.Update(sensor);
        await _context.SaveChangesAsync(tokenCancelamento);
    }

    public async Task<(List<Sensor> Itens, int Total)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        CancellationToken tokenCancelamento = default)
    {
        var query = _context.Sensores.AsQueryable();

        var total = await query.CountAsync(tokenCancelamento);

        var itens = await query
            .OrderByDescending(s => s.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .AsNoTracking()
            .ToListAsync(tokenCancelamento);

        return (itens, total);
    }

    public async Task<bool> DeletarAsync(int id, CancellationToken tokenCancelamento = default)
    {
        var sensor = await _context.Sensores.FindAsync(new object[] { id }, tokenCancelamento);

        if (sensor == null)
            return false;

        _context.Sensores.Remove(sensor);
        await _context.SaveChangesAsync(tokenCancelamento);
        return true;
    }
}
