using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.API.Controllers;

[ApiController]
[Route("api/v1/equipamentos")]
[Produces("application/json")]
public class EquipamentosController : ControllerBase
{
    private readonly ServicoMedicao _servicoMedicao;
    private readonly ServicoConsultaEquipamento _servicoConsultaEquipamento;

    public EquipamentosController(
        ServicoMedicao servicoMedicao,
        ServicoConsultaEquipamento servicoConsultaEquipamento)
    {
        _servicoMedicao = servicoMedicao;
        _servicoConsultaEquipamento = servicoConsultaEquipamento;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterEquipamentoPorId(int id, CancellationToken tokenCancelamento = default)
    {
        var resultado = await _servicoConsultaEquipamento.ObterPorIdAsync(id, tokenCancelamento);
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }

    [HttpGet]
    public async Task<IActionResult> ListarEquipamentos(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        CancellationToken tokenCancelamento = default)
    {
        var resultado = await _servicoConsultaEquipamento.ListarPaginadoAsync(pagina, tamanhoPagina, tokenCancelamento);
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> AtualizarEquipamento(
        int id,
        [FromBody] RequisicaoAtualizarEquipamento requisicao,
        CancellationToken tokenCancelamento = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(RespostaApi.De(ResultadoOperacao<bool>.Falha(
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)))));

        var resultado = await _servicoConsultaEquipamento.AtualizarAsync(id, requisicao, tokenCancelamento);
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletarEquipamento(int id, CancellationToken tokenCancelamento = default)
    {
        var resultado = await _servicoConsultaEquipamento.DeletarAsync(id, tokenCancelamento);
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }

    [HttpGet("{idEquipamento}/medicoes/ultimas")]
    public async Task<IActionResult> ObterUltimasMedicoes(
        int idEquipamento,
        CancellationToken tokenCancelamento)
    {
        var resultado = await _servicoMedicao.ObterMedicoesEquipamentoAsync(idEquipamento, tokenCancelamento);

        if (resultado == null)
            return NotFound(RespostaApi.De(ResultadoOperacao<object>.NaoEncontrado("Equipamento não encontrado")));

        return Ok(RespostaApi.De(ResultadoOperacao<object>.Ok(resultado, "Medições recuperadas com sucesso")));
    }
}
