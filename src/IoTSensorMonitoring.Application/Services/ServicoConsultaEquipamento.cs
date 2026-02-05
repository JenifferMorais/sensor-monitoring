using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Mappers;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IoTSensorMonitoring.Application.Services;

public class ServicoConsultaEquipamento
{
    private readonly IRepositorioEquipamento _repositorioEquipamento;
    private readonly ILogger<ServicoConsultaEquipamento> _logger;

    public ServicoConsultaEquipamento(IRepositorioEquipamento repositorioEquipamento, ILogger<ServicoConsultaEquipamento> logger)
    {
        _repositorioEquipamento = repositorioEquipamento;
        _logger = logger;
    }

    public async Task<ResultadoOperacao<DtoEquipamentoDetalhado>> ObterPorIdAsync(int id, CancellationToken tokenCancelamento = default)
    {
        try
        {
            var equipamento = await _repositorioEquipamento.ObterPorIdComSensoresAsync(id, tokenCancelamento);

            if (equipamento == null)
                return ResultadoOperacao<DtoEquipamentoDetalhado>.NaoEncontrado("Equipamento não encontrado");

            return ResultadoOperacao<DtoEquipamentoDetalhado>.Ok(equipamento.ParaDtoDetalhado(), "Equipamento recuperado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter equipamento {Id}", id);
            return ResultadoOperacao<DtoEquipamentoDetalhado>.ErroInterno();
        }
    }

    public async Task<ResultadoOperacao<DtoEquipamentoDetalhado>> CriarAsync(
        RequisicaoCriarEquipamento requisicao,
        CancellationToken tokenCancelamento = default)
    {
        try
        {
            var equipamento = requisicao.ParaEntidade();

            await _repositorioEquipamento.AdicionarAsync(equipamento, tokenCancelamento);

            _logger.LogInformation("Equipamento criado: {Nome} (ID: {Id})", equipamento.Nome, equipamento.Id);

            var equipamentoDetalhado = await _repositorioEquipamento.ObterPorIdComSensoresAsync(equipamento.Id, tokenCancelamento);

            return ResultadoOperacao<DtoEquipamentoDetalhado>.Ok(
                equipamentoDetalhado!.ParaDtoDetalhado(),
                "Equipamento criado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar equipamento {Nome}", requisicao.Nome);
            return ResultadoOperacao<DtoEquipamentoDetalhado>.ErroInterno();
        }
    }

    public async Task<ResultadoOperacao<RespostaPaginada<DtoEquipamento>>> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        CancellationToken tokenCancelamento = default)
    {
        try
        {
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1 || tamanhoPagina > 100) tamanhoPagina = 10;

            var (itens, total) = await _repositorioEquipamento.ListarPaginadoAsync(
                pagina, tamanhoPagina, tokenCancelamento);

            var resposta = itens.ParaDto().ParaRespostaPaginada(total, pagina, tamanhoPagina);
            return ResultadoOperacao<RespostaPaginada<DtoEquipamento>>.Ok(resposta, "Equipamentos recuperados com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar equipamentos");
            return ResultadoOperacao<RespostaPaginada<DtoEquipamento>>.ErroInterno();
        }
    }

    public async Task<ResultadoOperacao<bool>> AtualizarAsync(int id, RequisicaoAtualizarEquipamento requisicao, CancellationToken tokenCancelamento = default)
    {
        try
        {
            var equipamento = await _repositorioEquipamento.ObterPorIdAsync(id, tokenCancelamento);

            if (equipamento == null)
                return ResultadoOperacao<bool>.NaoEncontrado("Equipamento não encontrado");

            equipamento.Nome = requisicao.Nome;
            equipamento.Descricao = requisicao.Descricao;
            equipamento.IdSetor = requisicao.IdSetor;
            equipamento.EstaAtivo = requisicao.EstaAtivo;

            await _repositorioEquipamento.AtualizarAsync(equipamento, tokenCancelamento);
            return ResultadoOperacao<bool>.Ok(true, "Equipamento atualizado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar equipamento {Id}", id);
            return ResultadoOperacao<bool>.ErroInterno();
        }
    }

    public async Task<ResultadoOperacao<bool>> DeletarAsync(int id, CancellationToken tokenCancelamento = default)
    {
        try
        {
            var resultado = await _repositorioEquipamento.DeletarAsync(id, tokenCancelamento);

            if (!resultado)
                return ResultadoOperacao<bool>.NaoEncontrado("Equipamento não encontrado");

            return ResultadoOperacao<bool>.Ok(true, "Equipamento excluído");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar equipamento {Id}", id);
            return ResultadoOperacao<bool>.ErroInterno();
        }
    }
}
