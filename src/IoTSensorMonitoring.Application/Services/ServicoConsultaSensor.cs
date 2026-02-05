using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Mappers;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IoTSensorMonitoring.Application.Services;

public class ServicoConsultaSensor
{
    private readonly IRepositorioSensor _repositorioSensor;
    private readonly ILogger<ServicoConsultaSensor> _logger;

    public ServicoConsultaSensor(IRepositorioSensor repositorioSensor, ILogger<ServicoConsultaSensor> logger)
    {
        _repositorioSensor = repositorioSensor;
        _logger = logger;
    }

    public async Task<ResultadoOperacao<DtoSensor>> ObterPorIdAsync(int id, CancellationToken tokenCancelamento = default)
    {
        try
        {
            var sensor = await _repositorioSensor.ObterPorIdAsync(id, tokenCancelamento);

            if (sensor == null)
                return ResultadoOperacao<DtoSensor>.NaoEncontrado("Sensor não encontrado");

            return ResultadoOperacao<DtoSensor>.Ok(sensor.ParaDto(), "Sensor recuperado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter sensor {Id}", id);
            return ResultadoOperacao<DtoSensor>.ErroInterno();
        }
    }

    public async Task<ResultadoOperacao<RespostaPaginada<DtoSensor>>> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        CancellationToken tokenCancelamento = default)
    {
        try
        {
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1 || tamanhoPagina > 100) tamanhoPagina = 10;

            var (itens, total) = await _repositorioSensor.ListarPaginadoAsync(
                pagina, tamanhoPagina, tokenCancelamento);

            var resposta = itens.ParaDto().ParaRespostaPaginada(total, pagina, tamanhoPagina);
            return ResultadoOperacao<RespostaPaginada<DtoSensor>>.Ok(resposta, "Sensores recuperados com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar sensores");
            return ResultadoOperacao<RespostaPaginada<DtoSensor>>.ErroInterno();
        }
    }

    public async Task<ResultadoOperacao<bool>> AtualizarAsync(int id, RequisicaoAtualizarSensor requisicao, CancellationToken tokenCancelamento = default)
    {
        try
        {
            var sensor = await _repositorioSensor.ObterPorIdAsync(id, tokenCancelamento);

            if (sensor == null)
                return ResultadoOperacao<bool>.NaoEncontrado("Sensor não encontrado");

            sensor.Nome = requisicao.Nome;
            sensor.Descricao = requisicao.Descricao;
            sensor.EstaAtivo = requisicao.EstaAtivo;

            await _repositorioSensor.AtualizarAsync(sensor, tokenCancelamento);
            return ResultadoOperacao<bool>.Ok(true, "Sensor atualizado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar sensor {Id}", id);
            return ResultadoOperacao<bool>.ErroInterno();
        }
    }

    public async Task<ResultadoOperacao<bool>> DeletarAsync(int id, CancellationToken tokenCancelamento = default)
    {
        try
        {
            var resultado = await _repositorioSensor.DeletarAsync(id, tokenCancelamento);

            if (!resultado)
                return ResultadoOperacao<bool>.NaoEncontrado("Sensor não encontrado");

            return ResultadoOperacao<bool>.Ok(true, "Sensor excluído");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar sensor {Id}", id);
            return ResultadoOperacao<bool>.ErroInterno();
        }
    }
}
