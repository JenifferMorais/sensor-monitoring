using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.API.Controllers;

[ApiController]
[Route("api/v1/sensores")]
[Produces("application/json")]
public class SensoresController : ControllerBase
{
    private readonly ServicoVinculoSensorEquipamento _servicoSensorEquipamento;
    private readonly ServicoConsultaSensor _servicoConsultaSensor;

    public SensoresController(
        ServicoVinculoSensorEquipamento servicoSensorEquipamento,
        ServicoConsultaSensor servicoConsultaSensor)
    {
        _servicoSensorEquipamento = servicoSensorEquipamento;
        _servicoConsultaSensor = servicoConsultaSensor;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterSensorPorId(int id, CancellationToken tokenCancelamento = default)
    {
        var resultado = await _servicoConsultaSensor.ObterPorIdAsync(id, tokenCancelamento);
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }

    [HttpGet]
    public async Task<IActionResult> ListarSensores(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        CancellationToken tokenCancelamento = default)
    {
        var resultado = await _servicoConsultaSensor.ListarPaginadoAsync(pagina, tamanhoPagina, tokenCancelamento);
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> AtualizarSensor(
        int id,
        [FromBody] RequisicaoAtualizarSensor requisicao,
        CancellationToken tokenCancelamento = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(RespostaApi.De(ResultadoOperacao<bool>.Falha(
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)))));

        var resultado = await _servicoConsultaSensor.AtualizarAsync(id, requisicao, tokenCancelamento);
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletarSensor(int id, CancellationToken tokenCancelamento = default)
    {
        var resultado = await _servicoConsultaSensor.DeletarAsync(id, tokenCancelamento);
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }

    [HttpPost("{idSensor}/equipamento/{idEquipamento}/vincular")]
    public async Task<IActionResult> VincularSensorAoEquipamento(
        int idSensor,
        int idEquipamento,
        [FromBody] RequisicaoVincularSensor requisicao,
        CancellationToken tokenCancelamento)
    {
        if (string.IsNullOrWhiteSpace(requisicao.VinculadoPor))
            return BadRequest(RespostaApi.De(ResultadoOperacao<bool>.Falha("O campo 'VinculadoPor' é obrigatório")));

        var (sucesso, mensagem) = await _servicoSensorEquipamento.VincularSensorAoEquipamentoAsync(
            idSensor, idEquipamento, requisicao, tokenCancelamento);

        var resultado = sucesso
            ? ResultadoOperacao<bool>.Ok(true, mensagem)
            : ResultadoOperacao<bool>.Falha(mensagem);

        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }
}
