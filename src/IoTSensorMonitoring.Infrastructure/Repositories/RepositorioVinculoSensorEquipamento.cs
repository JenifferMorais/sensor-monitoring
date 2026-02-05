using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Repositories;

public class RepositorioVinculoSensorEquipamento : IRepositorioVinculoSensorEquipamento
{
    private readonly ApplicationDbContext _context;

    public RepositorioVinculoSensorEquipamento(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VinculoSensorEquipamento?> ObterVinculoAtivoAsync(int idSensor, int idEquipamento, CancellationToken tokenCancelamento = default)
    {
        return await _context.VinculosSensorEquipamento
            .FirstOrDefaultAsync(vse =>
                vse.IdSensor == idSensor &&
                vse.IdEquipamento == idEquipamento &&
                vse.EstaAtivo,
                tokenCancelamento);
    }

    public async Task<VinculoSensorEquipamento> AdicionarAsync(VinculoSensorEquipamento vinculo, CancellationToken tokenCancelamento = default)
    {
        await _context.VinculosSensorEquipamento.AddAsync(vinculo, tokenCancelamento);
        await _context.SaveChangesAsync(tokenCancelamento);
        return vinculo;
    }
}
