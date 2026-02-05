using System.Text.Json;
using IoTSensorMonitoring.Application.Mappers;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IoTSensorMonitoring.Application.Services;

public class ServicoAvaliacaoAlerta
{
    private readonly IRepositorioRegraAlerta _alertRuleRepository;
    private readonly IRepositorioEstadoAlerta _alertStateRepository;
    private readonly IRepositorioHistoricoAlerta _alertHistoryRepository;
    private readonly ILogger<ServicoAvaliacaoAlerta> _logger;

    public ServicoAvaliacaoAlerta(
        IRepositorioRegraAlerta alertRuleRepository,
        IRepositorioEstadoAlerta alertStateRepository,
        IRepositorioHistoricoAlerta alertHistoryRepository,
        ILogger<ServicoAvaliacaoAlerta> logger)
    {
        _alertRuleRepository = alertRuleRepository;
        _alertStateRepository = alertStateRepository;
        _alertHistoryRepository = alertHistoryRepository;
        _logger = logger;
    }

    public async Task<List<AlertaDisparado>> AvaliarAlertasAsync(
        int idSensor,
        decimal medicao,
        DateTimeOffset horarioMedicao,
        CancellationToken tokenCancelamento = default)
    {
        var regras = await _alertRuleRepository.ObterAtivasPorIdSensorAsync(idSensor, tokenCancelamento);

        if (!regras.Any())
        {
            return new List<AlertaDisparado>();
        }

        _logger.LogDebug("Avaliando {Count} regra(s) de alerta para sensor ID {SensorId}", regras.Count, idSensor);

        var alertasDisparados = new List<AlertaDisparado>();

        foreach (var regra in regras)
        {
            try
            {
                if (regra.TipoRegra == TipoRegraAlerta.ConsecutivoForaIntervalo)
                {
                    var disparado = await AvaliarConsecutivoForaIntervaloAsync(
                        idSensor, regra, medicao, horarioMedicao, tokenCancelamento);
                    if (disparado != null)
                    {
                        alertasDisparados.Add(disparado);
                    }
                }
                else if (regra.TipoRegra == TipoRegraAlerta.MediaMargemErro)
                {
                    var disparado = await AvaliarMediaMargemErroAsync(
                        idSensor, regra, medicao, horarioMedicao, tokenCancelamento);
                    if (disparado != null)
                    {
                        alertasDisparados.Add(disparado);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao avaliar regra {RuleId} para sensor {SensorId}", regra.Id, idSensor);
            }
        }

        return alertasDisparados;
    }

    private async Task<AlertaDisparado?> AvaliarConsecutivoForaIntervaloAsync(
        int idSensor,
        RegraAlerta regra,
        decimal medicao,
        DateTimeOffset horarioMedicao,
        CancellationToken tokenCancelamento)
    {
        var estado = await ObterOuCriarEstadoAlertaAsync(idSensor, regra.Id, tokenCancelamento);

        bool estaForaIntervalo = medicao < regra.LimiteMinimo || medicao > regra.LimiteMaximo;

        if (estaForaIntervalo)
        {
            estado.ContagemConsecutiva++;
            _logger.LogDebug("Sensor {SensorId} fora do intervalo: contagem consecutiva = {Count}",
                idSensor, estado.ContagemConsecutiva);

            if (estado.ContagemConsecutiva >= regra.ContagemConsecutiva)
            {
                var motivoDisparo = $"Fora do intervalo consecutivo: {estado.ContagemConsecutiva} medições fora de [{regra.LimiteMinimo}, {regra.LimiteMaximo}]";
                var alerta = regra.ParaAlertaDisparado(idSensor, motivoDisparo, medicao, horarioMedicao);

                _logger.LogWarning("ALERTA disparado para sensor {SensorId}: {Motivo}", idSensor, alerta.MotivoDisparo);

                estado.ContagemConsecutiva = 0;
                estado.UltimaAtualizacao = DateTimeOffset.UtcNow;
                await _alertStateRepository.InserirOuAtualizarAsync(estado, tokenCancelamento);

                await _alertHistoryRepository.AdicionarAsync(
                    alerta.ParaHistoricoAlerta(idSensor, horarioMedicao), tokenCancelamento);

                return alerta;
            }
        }
        else
        {
            if (estado.ContagemConsecutiva > 0)
            {
                _logger.LogDebug("Sensor {SensorId} voltou ao intervalo normal, resetando contagem", idSensor);
            }
            estado.ContagemConsecutiva = 0;
        }

        estado.UltimaAtualizacao = DateTimeOffset.UtcNow;
        await _alertStateRepository.InserirOuAtualizarAsync(estado, tokenCancelamento);
        return null;
    }

    private async Task<AlertaDisparado?> AvaliarMediaMargemErroAsync(
        int idSensor,
        RegraAlerta regra,
        decimal medicao,
        DateTimeOffset horarioMedicao,
        CancellationToken tokenCancelamento)
    {
        var estado = await ObterOuCriarEstadoAlertaAsync(idSensor, regra.Id, tokenCancelamento);

        List<decimal> medicoesRecentes;
        try
        {
            medicoesRecentes = JsonSerializer.Deserialize<List<decimal>>(estado.JsonMedicoesRecentes) ?? new List<decimal>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Falha ao deserializar medições recentes do sensor {SensorId}, reiniciando janela", idSensor);
            medicoesRecentes = new List<decimal>();
        }

        medicoesRecentes.Add(medicao);
        while (medicoesRecentes.Count > regra.TamanhoJanelaMedia)
        {
            medicoesRecentes.RemoveAt(0);
        }

        estado.JsonMedicoesRecentes = JsonSerializer.Serialize(medicoesRecentes);
        estado.UltimaAtualizacao = DateTimeOffset.UtcNow;
        await _alertStateRepository.InserirOuAtualizarAsync(estado, tokenCancelamento);

        if (medicoesRecentes.Count >= regra.TamanhoJanelaMedia)
        {
            decimal media = medicoesRecentes.Average();

            bool naMargemInferior = media >= (regra.LimiteMinimo - regra.MargemErro) &&
                                media <= (regra.LimiteMinimo + regra.MargemErro);

            bool naMargemSuperior = media >= (regra.LimiteMaximo - regra.MargemErro) &&
                                media <= (regra.LimiteMaximo + regra.MargemErro);

            if (naMargemInferior || naMargemSuperior)
            {
                var tipoMargem = naMargemInferior ? "inferior" : "superior";
                var motivoDisparo = $"Média na margem de erro {tipoMargem}: {media:F2} (margem: ±{regra.MargemErro})";
                var alerta = regra.ParaAlertaDisparado(idSensor, motivoDisparo, media, horarioMedicao);

                _logger.LogWarning("ALERTA disparado para sensor {SensorId}: {Motivo}", idSensor, alerta.MotivoDisparo);

                await _alertHistoryRepository.AdicionarAsync(
                    alerta.ParaHistoricoAlerta(idSensor, horarioMedicao), tokenCancelamento);

                return alerta;
            }
        }

        return null;
    }

    private async Task<EstadoAlerta> ObterOuCriarEstadoAlertaAsync(
        int idSensor,
        int idRegraAlerta,
        CancellationToken tokenCancelamento)
    {
        var estadoExistente = await _alertStateRepository.ObterPorRegraAlertaAsync(idSensor, idRegraAlerta, tokenCancelamento);

        if (estadoExistente != null)
        {
            return estadoExistente;
        }

        var novoEstado = CriarEstadoAlertaInicial(idSensor, idRegraAlerta);
        return novoEstado;
    }

    private EstadoAlerta CriarEstadoAlertaInicial(int idSensor, int idRegra)
    {
        return new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = idRegra,
            ContagemConsecutiva = 0,
            JsonMedicoesRecentes = "[]",
            UltimaAtualizacao = DateTimeOffset.UtcNow
        };
    }
}

public class AlertaDisparado
{
    public int IdSensor { get; set; }
    public int IdRegraAlerta { get; set; }
    public TipoRegraAlerta TipoRegra { get; set; }
    public string MotivoDisparo { get; set; } = string.Empty;
    public decimal? ValorDisparo { get; set; }
    public string EmailNotificacao { get; set; } = string.Empty;
    public DateTimeOffset DisparadoEm { get; set; }
}
