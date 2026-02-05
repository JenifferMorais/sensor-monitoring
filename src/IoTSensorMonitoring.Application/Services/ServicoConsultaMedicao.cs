using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Mappers;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IoTSensorMonitoring.Application.Services;

public class ServicoConsultaMedicao
{
    private readonly IRepositorioMedicao _repositorioMedicao;
    private readonly ILogger<ServicoConsultaMedicao> _logger;

    public ServicoConsultaMedicao(IRepositorioMedicao repositorioMedicao, ILogger<ServicoConsultaMedicao> logger)
    {
        _repositorioMedicao = repositorioMedicao;
        _logger = logger;
    }

    public async Task<ResultadoOperacao<DtoMedicaoCompleta>> ObterPorIdAsync(int id, CancellationToken tokenCancelamento = default)
    {
        try
        {
            var medicao = await _repositorioMedicao.ObterPorIdAsync(id, tokenCancelamento);

            if (medicao == null)
                return ResultadoOperacao<DtoMedicaoCompleta>.NaoEncontrado("Medição não encontrada");

            return ResultadoOperacao<DtoMedicaoCompleta>.Ok(medicao.ParaDtoCompleto(), "Medição recuperada com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter medição {Id}", id);
            return ResultadoOperacao<DtoMedicaoCompleta>.ErroInterno();
        }
    }

    public async Task<ResultadoOperacao<RespostaPaginada<DtoMedicaoCompleta>>> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        int? idSensor = null,
        CancellationToken tokenCancelamento = default)
    {
        try
        {
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1 || tamanhoPagina > 100) tamanhoPagina = 10;

            var (itens, total) = await _repositorioMedicao.ListarPaginadoAsync(
                pagina, tamanhoPagina, idSensor, tokenCancelamento);

            var resposta = itens.ParaDtoCompleto().ParaRespostaPaginada(total, pagina, tamanhoPagina);
            return ResultadoOperacao<RespostaPaginada<DtoMedicaoCompleta>>.Ok(resposta, "Medições recuperadas com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar medições");
            return ResultadoOperacao<RespostaPaginada<DtoMedicaoCompleta>>.ErroInterno();
        }
    }
}
