using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Repositories;

public class RepositorioEstadoAlerta : IRepositorioEstadoAlerta
{
    private readonly ApplicationDbContext _context;

    public RepositorioEstadoAlerta(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EstadoAlerta?> ObterPorRegraAlertaAsync(int idSensor, int idRegraAlerta, CancellationToken tokenCancelamento = default)
    {
        return await _context.EstadosAlerta
            .FirstOrDefaultAsync(ea =>
                ea.IdSensor == idSensor &&
                ea.IdRegraAlerta == idRegraAlerta,
                tokenCancelamento);
    }

    public async Task InserirOuAtualizarAsync(EstadoAlerta estado, CancellationToken tokenCancelamento = default)
    {
        var existente = await ObterPorRegraAlertaAsync(estado.IdSensor, estado.IdRegraAlerta, tokenCancelamento);

        if (existente == null)
        {
            await _context.EstadosAlerta.AddAsync(estado, tokenCancelamento);
        }
        else
        {
            existente.ContagemConsecutiva = estado.ContagemConsecutiva;
            existente.JsonMedicoesRecentes = estado.JsonMedicoesRecentes;
            existente.UltimaAtualizacao = estado.UltimaAtualizacao;
            _context.EstadosAlerta.Update(existente);
        }

        await _context.SaveChangesAsync(tokenCancelamento);
    }
}
