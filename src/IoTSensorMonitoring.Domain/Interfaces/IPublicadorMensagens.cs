namespace IoTSensorMonitoring.Domain.Interfaces;

public interface IPublicadorMensagens
{
    void Publicar<T>(string routingKey, T mensagem) where T : class;
    Task PublicarAsync<T>(string routingKey, T mensagem, CancellationToken cancellationToken = default) where T : class;
}
