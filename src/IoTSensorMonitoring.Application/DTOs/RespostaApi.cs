namespace IoTSensorMonitoring.Application.DTOs;

public class RespostaApi
{
    public int Codigo { get; set; }
    public bool Sucesso { get; set; }
    public string? Mensagem { get; set; }
    public string? Erro { get; set; }
    public object? Dados { get; set; }

    public static RespostaApi De<T>(ResultadoOperacao<T> resultado)
    {
        return new RespostaApi
        {
            Codigo = resultado.CodigoHttp,
            Sucesso = resultado.Sucesso,
            Mensagem = resultado.Mensagem,
            Erro = resultado.Erro,
            Dados = resultado.Dados
        };
    }
}
