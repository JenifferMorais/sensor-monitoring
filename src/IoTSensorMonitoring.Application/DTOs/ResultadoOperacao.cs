namespace IoTSensorMonitoring.Application.DTOs;

public class ResultadoOperacao<T>
{
    public bool Sucesso { get; set; }
    public T? Dados { get; set; }
    public string? Mensagem { get; set; }
    public string? Erro { get; set; }
    public int CodigoHttp { get; set; }

    public static ResultadoOperacao<T> Ok(T dados, string mensagem = "Operação realizada com sucesso")
    {
        return new ResultadoOperacao<T>
        {
            Sucesso = true,
            Dados = dados,
            Mensagem = mensagem,
            CodigoHttp = 200
        };
    }

    public static ResultadoOperacao<T> NaoEncontrado(string mensagem = "Recurso não encontrado")
    {
        return new ResultadoOperacao<T>
        {
            Sucesso = false,
            Erro = mensagem,
            CodigoHttp = 404
        };
    }

    public static ResultadoOperacao<T> Falha(string erro, int codigoHttp = 400)
    {
        return new ResultadoOperacao<T>
        {
            Sucesso = false,
            Erro = erro,
            CodigoHttp = codigoHttp
        };
    }

    public static ResultadoOperacao<T> ErroInterno(string erro = "Erro ao processar requisição")
    {
        return new ResultadoOperacao<T>
        {
            Sucesso = false,
            Erro = erro,
            CodigoHttp = 500
        };
    }
}
