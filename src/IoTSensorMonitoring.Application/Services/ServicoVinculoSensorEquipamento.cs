using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IoTSensorMonitoring.Application.Services;

public class ServicoVinculoSensorEquipamento
{
    private readonly IRepositorioSensor _sensorRepository;
    private readonly IRepositorioEquipamento _equipmentRepository;
    private readonly IRepositorioVinculoSensorEquipamento _linkRepository;
    private readonly ILogger<ServicoVinculoSensorEquipamento> _logger;

    public ServicoVinculoSensorEquipamento(
        IRepositorioSensor sensorRepository,
        IRepositorioEquipamento equipmentRepository,
        IRepositorioVinculoSensorEquipamento linkRepository,
        ILogger<ServicoVinculoSensorEquipamento> logger)
    {
        _sensorRepository = sensorRepository;
        _equipmentRepository = equipmentRepository;
        _linkRepository = linkRepository;
        _logger = logger;
    }

    public async Task<(bool Sucesso, string Mensagem)> VincularSensorAoEquipamentoAsync(
        int idSensor,
        int idEquipamento,
        RequisicaoVincularSensor requisicao,
        CancellationToken tokenCancelamento = default)
    {
        _logger.LogInformation("Tentando vincular sensor {SensorId} ao equipamento {EquipmentId}", idSensor, idEquipamento);

        var sensor = await _sensorRepository.ObterPorIdAsync(idSensor, tokenCancelamento);
        if (sensor == null)
        {
            _logger.LogWarning("Sensor {SensorId} não encontrado para vinculação", idSensor);
            return (false, "Sensor não encontrado");
        }

        var equipamento = await _equipmentRepository.ObterPorIdAsync(idEquipamento, tokenCancelamento);
        if (equipamento == null)
        {
            _logger.LogWarning("Equipamento {EquipmentId} não encontrado para vinculação", idEquipamento);
            return (false, "Equipamento não encontrado");
        }

        var vinculoExistente = await _linkRepository.ObterVinculoAtivoAsync(idSensor, idEquipamento, tokenCancelamento);
        if (vinculoExistente != null)
        {
            _logger.LogWarning("Vínculo já existe entre sensor {SensorId} e equipamento {EquipmentId}", idSensor, idEquipamento);
            return (false, "Sensor já está vinculado a este equipamento");
        }

        var vinculo = new VinculoSensorEquipamento
        {
            IdSensor = idSensor,
            IdEquipamento = idEquipamento,
            VinculadoPor = requisicao.VinculadoPor,
            VinculadoEm = DateTimeOffset.UtcNow,
            EstaAtivo = true
        };

        await _linkRepository.AdicionarAsync(vinculo, tokenCancelamento);

        _logger.LogInformation("Sensor {SensorCodigo} vinculado ao equipamento {EquipmentNome} por {Usuario}",
            sensor.Codigo, equipamento.Nome, requisicao.VinculadoPor);

        return (true, "Sensor vinculado com sucesso");
    }
}
