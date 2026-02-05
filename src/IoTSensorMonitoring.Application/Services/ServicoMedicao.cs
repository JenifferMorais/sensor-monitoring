using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Mappers;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IoTSensorMonitoring.Application.Services;

public class ServicoMedicao
{
    private readonly IRepositorioMedicao _measurementRepository;
    private readonly IRepositorioSensor _sensorRepository;
    private readonly IRepositorioEquipamento _equipmentRepository;
    private readonly ServicoAvaliacaoAlerta _avaliadorAlerta;
    private readonly IPublicadorMensagens? _publicador;
    private readonly ILogger<ServicoMedicao> _logger;

    public ServicoMedicao(
        IRepositorioMedicao measurementRepository,
        IRepositorioSensor sensorRepository,
        IRepositorioEquipamento equipmentRepository,
        ServicoAvaliacaoAlerta avaliadorAlerta,
        ILogger<ServicoMedicao> logger,
        IPublicadorMensagens? publicador = null)
    {
        _measurementRepository = measurementRepository;
        _sensorRepository = sensorRepository;
        _equipmentRepository = equipmentRepository;
        _avaliadorAlerta = avaliadorAlerta;
        _publicador = publicador;
        _logger = logger;
    }

    public async Task<(bool Sucesso, List<AlertaDisparado> Alertas)> ProcessarMedicaoAsync(
        RequisicaoMedicaoUnica requisicao,
        CancellationToken tokenCancelamento = default)
    {
        _logger.LogDebug("Processando medição do sensor {Codigo} (ID: {Id})", requisicao.Codigo, requisicao.Id);

        var sensor = await EncontrarOuCriarSensorAsync(requisicao.Id, requisicao.Codigo, tokenCancelamento);

        var medicao = new Medicao
        {
            IdSensor = sensor.Id,
            DataHoraMedicao = requisicao.DataHoraMedicao,
            ValorMedicao = requisicao.Medicao,
            RecebidoEm = DateTimeOffset.UtcNow
        };

        await _measurementRepository.AdicionarAsync(medicao, tokenCancelamento);
        _logger.LogDebug("Medição salva: {Valor} em {DataHora}", requisicao.Medicao, requisicao.DataHoraMedicao);

        if (_publicador != null)
        {
            var mensagem = medicao.ParaMensagem(sensor);
            _publicador.Publicar($"medicao.{sensor.Codigo}", mensagem);
            _logger.LogDebug("Medição publicada no RabbitMQ");

            return (true, new List<AlertaDisparado>());
        }

        var alertas = await _avaliadorAlerta.AvaliarAlertasAsync(
            sensor.Id,
            requisicao.Medicao,
            requisicao.DataHoraMedicao,
            tokenCancelamento);

        if (alertas.Any())
        {
            _logger.LogWarning("Sensor {Codigo} disparou {Count} alerta(s)", requisicao.Codigo, alertas.Count);
        }

        return (true, alertas);
    }

    private async Task<Sensor> EncontrarOuCriarSensorAsync(int id, string codigo, CancellationToken tokenCancelamento)
    {
        var sensor = await _sensorRepository.ObterPorIdAsync(id, tokenCancelamento)
                     ?? await _sensorRepository.ObterPorCodigoAsync(codigo, tokenCancelamento);

        if (sensor == null)
        {
            _logger.LogInformation("Criando novo sensor com código {Codigo}", codigo);
            sensor = new Sensor
            {
                Id = id,
                Codigo = codigo,
                Nome = $"Sensor {codigo}",
                EstaAtivo = true
            };
            await _sensorRepository.AdicionarAsync(sensor, tokenCancelamento);
        }

        return sensor;
    }

    public async Task<(bool Sucesso, List<AlertaDisparado> Alertas)> ProcessarLoteMedicoesAsync(
        RequisicaoLoteMedicoes requisicao,
        CancellationToken tokenCancelamento = default)
    {
        var idLote = Guid.NewGuid();
        _logger.LogInformation("Iniciando processamento de lote com {Count} medições (BatchId: {BatchId})",
            requisicao.Medicoes.Count, idLote);

        var medicoes = new List<Medicao>();
        var todosAlertas = new List<AlertaDisparado>();
        var sensoresCache = new Dictionary<string, Sensor>();
        var contagemNovosSensores = 0;

        foreach (var item in requisicao.Medicoes)
        {
            Sensor? sensorAtual;

            if (!sensoresCache.TryGetValue(item.Codigo, out sensorAtual))
            {
                sensorAtual = await _sensorRepository.ObterPorIdAsync(item.Id, tokenCancelamento)
                         ?? await _sensorRepository.ObterPorCodigoAsync(item.Codigo, tokenCancelamento);

                if (sensorAtual == null)
                {
                    sensorAtual = new Sensor
                    {
                        Id = item.Id,
                        Codigo = item.Codigo,
                        Nome = $"Sensor {item.Codigo}",
                        EstaAtivo = true
                    };
                    await _sensorRepository.AdicionarAsync(sensorAtual, tokenCancelamento);
                    contagemNovosSensores++;
                }

                sensoresCache[item.Codigo] = sensorAtual;
            }

            medicoes.Add(new Medicao
            {
                IdSensor = sensorAtual.Id,
                DataHoraMedicao = item.DataHoraMedicao,
                ValorMedicao = item.Medicao,
                RecebidoEm = DateTimeOffset.UtcNow,
                IdLote = idLote
            });
        }

        if (contagemNovosSensores > 0)
        {
            _logger.LogInformation("Criados {Count} novos sensores durante o processamento do lote", contagemNovosSensores);
        }

        await _measurementRepository.AdicionarLoteAsync(medicoes, tokenCancelamento);
        _logger.LogDebug("Medições persistidas no banco de dados");

        if (_publicador != null)
        {
            for (int i = 0; i < requisicao.Medicoes.Count; i++)
            {
                var item = requisicao.Medicoes[i];
                var medicao = medicoes[i];
                var sensor = sensoresCache[item.Codigo];

                var mensagem = medicao.ParaMensagem(sensor);
                _publicador.Publicar($"medicao.{sensor.Codigo}", mensagem);
            }

            _logger.LogInformation("Lote processado com sucesso: {Count} medições publicadas no RabbitMQ", medicoes.Count);
            return (true, new List<AlertaDisparado>());
        }

        for (int i = 0; i < requisicao.Medicoes.Count; i++)
        {
            var item = requisicao.Medicoes[i];
            var medicao = medicoes[i];

            var alertasDisparados = await _avaliadorAlerta.AvaliarAlertasAsync(
                medicao.IdSensor,
                item.Medicao,
                item.DataHoraMedicao,
                tokenCancelamento);

            todosAlertas.AddRange(alertasDisparados);
        }

        _logger.LogInformation("Lote processado com sucesso: {TotalAlertas} alerta(s) disparado(s)", todosAlertas.Count);
        return (true, todosAlertas);
    }

    public async Task<RespostaMedicoesEquipamento?> ObterMedicoesEquipamentoAsync(
        int idEquipamento,
        CancellationToken tokenCancelamento = default)
    {
        _logger.LogDebug("Buscando medições para equipamento ID: {EquipmentId}", idEquipamento);

        var equipamento = await _equipmentRepository.ObterPorIdComSensoresAsync(idEquipamento, tokenCancelamento);

        if (equipamento == null)
        {
            _logger.LogWarning("Equipamento {EquipmentId} não encontrado", idEquipamento);
            return null;
        }

        var medicoes = await _measurementRepository.ObterUltimasPorEquipamentoAsync(idEquipamento, 10, tokenCancelamento);
        _logger.LogDebug("Recuperadas {Count} medições para o equipamento {Nome}",
            medicoes.Count, equipamento.Nome);

        var gruposSensor = medicoes
            .GroupBy(m => m.IdSensor)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.DataHoraMedicao).Take(10).ToList());

        var resposta = equipamento.ParaRespostaMedicoesEquipamento(gruposSensor);

        _logger.LogDebug("Retornando dados de {SensorCount} sensores para equipamento {Nome}",
            resposta.Sensores.Count, equipamento.Nome);

        return resposta;
    }
}
