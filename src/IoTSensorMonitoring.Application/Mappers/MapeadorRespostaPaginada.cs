using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Mappers;

public static class MapeadorRespostaPaginada
{
    public static RespostaPaginada<T> ParaRespostaPaginada<T>(
        this IEnumerable<T> itens,
        int total,
        int pagina,
        int tamanhoPagina)
    {
        return new RespostaPaginada<T>
        {
            Itens = itens.ToList(),
            PaginaAtual = pagina,
            TamanhoPagina = tamanhoPagina,
            TotalItens = total
        };
    }
}
