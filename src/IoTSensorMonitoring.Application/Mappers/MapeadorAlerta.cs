using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Mappers;

public static class MapeadorAlerta
{
    public static HistoricoAlerta ParaHistoricoAlerta(
        this AlertaDisparado alerta,
        int idSensor,
        DateTimeOffset horarioDisparo)
    {
        return new HistoricoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = alerta.IdRegraAlerta,
            TipoRegra = alerta.TipoRegra,
            MotivoDisparo = alerta.MotivoDisparo,
            ValorDisparo = alerta.ValorDisparo,
            DisparadoEm = horarioDisparo,
            EmailEnviado = false
        };
    }

    public static AlertaDisparado ParaAlertaDisparado(
        this RegraAlerta regra,
        int idSensor,
        string motivoDisparo,
        decimal valorDisparo,
        DateTimeOffset horarioDisparo)
    {
        return new AlertaDisparado
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra.Id,
            TipoRegra = regra.TipoRegra,
            MotivoDisparo = motivoDisparo,
            ValorDisparo = valorDisparo,
            EmailNotificacao = regra.EmailNotificacao,
            DisparadoEm = horarioDisparo
        };
    }
}
