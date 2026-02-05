using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Repositories;

public class RepositorioEquipamento : IRepositorioEquipamento
{
    private readonly ApplicationDbContext _context;

    public RepositorioEquipamento(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Equipamento?> ObterPorIdAsync(int id, CancellationToken tokenCancelamento = default)
    {
        return await _context.Equipamentos
            .FirstOrDefaultAsync(e => e.Id == id, tokenCancelamento);
    }

    public async Task<Equipamento?> ObterPorIdComSensoresAsync(int id, CancellationToken tokenCancelamento = default)
    {
        return await _context.Equipamentos
            .Include(e => e.VinculosSensorEquipamento.Where(vse => vse.EstaAtivo))
            .ThenInclude(vse => vse.Sensor)
            .FirstOrDefaultAsync(e => e.Id == id, tokenCancelamento);
    }

    public async Task<(List<Equipamento> Itens, int Total)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        CancellationToken tokenCancelamento = default)
    {
        var query = _context.Equipamentos
            .Include(e => e.Setor)
            .Include(e => e.VinculosSensorEquipamento.Where(vse => vse.EstaAtivo))
            .AsQueryable();

        var total = await query.CountAsync(tokenCancelamento);

        var itens = await query
            .OrderBy(e => e.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .AsNoTracking()
            .ToListAsync(tokenCancelamento);

        return (itens, total);
    }

    public async Task AtualizarAsync(Equipamento equipamento, CancellationToken tokenCancelamento = default)
    {
        _context.Equipamentos.Update(equipamento);
        await _context.SaveChangesAsync(tokenCancelamento);
    }

    public async Task<bool> DeletarAsync(int id, CancellationToken tokenCancelamento = default)
    {
        var equipamento = await _context.Equipamentos.FindAsync(new object[] { id }, tokenCancelamento);

        if (equipamento == null)
            return false;

        _context.Equipamentos.Remove(equipamento);
        await _context.SaveChangesAsync(tokenCancelamento);
        return true;
    }
}
