using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Repositories;

public class RepositorioMedicao : IRepositorioMedicao
{
    private readonly ApplicationDbContext _context;

    public RepositorioMedicao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Medicao?> ObterPorIdAsync(int id, CancellationToken tokenCancelamento = default)
    {
        return await _context.Medicoes
            .Include(m => m.Sensor)
            .FirstOrDefaultAsync(m => m.Id == id, tokenCancelamento);
    }

    public async Task<Medicao> AdicionarAsync(Medicao medicao, CancellationToken tokenCancelamento = default)
    {
        await _context.Medicoes.AddAsync(medicao, tokenCancelamento);
        await _context.SaveChangesAsync(tokenCancelamento);
        return medicao;
    }

    public async Task AdicionarLoteAsync(IEnumerable<Medicao> medicoes, CancellationToken tokenCancelamento = default)
    {
        await _context.Medicoes.AddRangeAsync(medicoes, tokenCancelamento);
        await _context.SaveChangesAsync(tokenCancelamento);
    }

    public async Task<List<Medicao>> ObterUltimasPorEquipamentoAsync(int idEquipamento, int limitePorSensor = 10, CancellationToken tokenCancelamento = default)
    {
        var sql = @"
            WITH MedicoesClassificadas AS (
                SELECT
                    m.""Id"", m.""IdSensor"", m.""DataHoraMedicao"", m.""ValorMedicao"", m.""RecebidoEm"",
                    ROW_NUMBER() OVER (PARTITION BY m.""IdSensor"" ORDER BY m.""DataHoraMedicao"" DESC) as rn
                FROM ""Medicoes"" m
                INNER JOIN ""VinculosSensorEquipamento"" vse ON m.""IdSensor"" = vse.""IdSensor""
                WHERE vse.""IdEquipamento"" = {0} AND vse.""EstaAtivo"" = true
            )
            SELECT ""Id"", ""IdSensor"", ""DataHoraMedicao"", ""ValorMedicao"", ""RecebidoEm""
            FROM MedicoesClassificadas
            WHERE rn <= {1}
            ORDER BY ""IdSensor"", ""DataHoraMedicao"" DESC";

        return await _context.Medicoes
            .FromSqlRaw(sql, idEquipamento, limitePorSensor)
            .AsNoTracking()
            .ToListAsync(tokenCancelamento);
    }

    public async Task<List<Medicao>> ObterRecentesPorSensorAsync(int idSensor, int quantidade, CancellationToken tokenCancelamento = default)
    {
        return await _context.Medicoes
            .Where(m => m.IdSensor == idSensor)
            .OrderByDescending(m => m.DataHoraMedicao)
            .Take(quantidade)
            .AsNoTracking()
            .ToListAsync(tokenCancelamento);
    }

    public async Task<(List<Medicao> Itens, int Total)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        int? idSensor = null,
        CancellationToken tokenCancelamento = default)
    {
        var query = _context.Medicoes
            .Include(m => m.Sensor)
            .AsQueryable();

        if (idSensor.HasValue)
        {
            query = query.Where(m => m.IdSensor == idSensor.Value);
        }

        var total = await query.CountAsync(tokenCancelamento);

        var itens = await query
            .OrderByDescending(m => m.DataHoraMedicao)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .AsNoTracking()
            .ToListAsync(tokenCancelamento);

        return (itens, total);
    }
}
