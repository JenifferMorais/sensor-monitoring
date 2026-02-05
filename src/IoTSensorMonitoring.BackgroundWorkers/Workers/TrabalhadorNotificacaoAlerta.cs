using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IoTSensorMonitoring.BackgroundWorkers.Workers;

public class TrabalhadorNotificacaoAlerta : BackgroundService
{
    private readonly ILogger<TrabalhadorNotificacaoAlerta> _logger;
    private readonly IServiceProvider _provedorServico;

    public TrabalhadorNotificacaoAlerta(
        ILogger<TrabalhadorNotificacaoAlerta> logger,
        IServiceProvider provedorServico)
    {
        _logger = logger;
        _provedorServico = provedorServico;
    }

    protected override async Task ExecuteAsync(CancellationToken tokenParada)
    {
        _logger.LogInformation("Worker de notificação de alertas iniciado");

        while (!tokenParada.IsCancellationRequested)
        {
            try
            {
                await ProcessarAlertasPendentesAsync(tokenParada);
                await Task.Delay(TimeSpan.FromSeconds(30), tokenParada);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker de notificação sendo encerrado...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro não tratado no worker de notificação");
                await Task.Delay(TimeSpan.FromMinutes(1), tokenParada);
            }
        }

        _logger.LogInformation("Worker de notificação de alertas encerrado");
    }

    private async Task ProcessarAlertasPendentesAsync(CancellationToken tokenCancelamento)
    {
        using var escopo = _provedorServico.CreateScope();
        var repositorioHistoricoAlerta = escopo.ServiceProvider.GetRequiredService<IRepositorioHistoricoAlerta>();

        var alertasPendentes = await repositorioHistoricoAlerta.ObterAlertasEmailPendentesAsync(50, tokenCancelamento);

        if (!alertasPendentes.Any())
        {
            return;
        }

        _logger.LogInformation("Processando {Count} alerta(s) pendente(s)", alertasPendentes.Count);

        foreach (var alerta in alertasPendentes)
        {
            try
            {
                await EnviarNotificacaoEmailAsync(alerta);

                alerta.EmailEnviado = true;
                alerta.EmailEnviadoEm = DateTimeOffset.UtcNow;
                await repositorioHistoricoAlerta.AtualizarAsync(alerta, tokenCancelamento);

                _logger.LogInformation("Email de alerta enviado para {Email} (Sensor ID: {SensorId})",
                    alerta.RegraAlerta?.EmailNotificacao ?? "N/A", alerta.IdSensor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar email para alerta {AlertId}", alerta.Id);
            }
        }
    }

    private async Task EnviarNotificacaoEmailAsync(Domain.Entities.HistoricoAlerta alerta)
    {
        await Task.Delay(100);

        _logger.LogDebug("Email simulado enviado: {Motivo}", alerta.MotivoDisparo);
    }
}
