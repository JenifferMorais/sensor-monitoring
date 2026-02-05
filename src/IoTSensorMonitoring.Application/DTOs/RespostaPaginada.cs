namespace IoTSensorMonitoring.Application.DTOs;

public class RespostaPaginada<T>
{
    public List<T> Itens { get; set; } = new();
    public int PaginaAtual { get; set; }
    public int TamanhoPagina { get; set; }
    public int TotalItens { get; set; }
    public int TotalPaginas => (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
    public bool TemPaginaAnterior => PaginaAtual > 1;
    public bool TemProximaPagina => PaginaAtual < TotalPaginas;
}
