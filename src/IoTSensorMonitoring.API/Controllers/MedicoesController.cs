using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.API.Controllers;

[ApiController]
[Route("api/v1/medicoes")]
[Produces("application/json")]
public class MedicoesController : ControllerBase
{
    private readonly ServicoMedicao _servicoMedicao;
    private readonly ServicoConsultaMedicao _servicoConsultaMedicao;
    private readonly IValidator<RequisicaoMedicaoUnica> _validadorUnico;
    private readonly IValidator<RequisicaoLoteMedicoes> _validadorLote;

    public MedicoesController(
        ServicoMedicao servicoMedicao,
        ServicoConsultaMedicao servicoConsultaMedicao,
        IValidator<RequisicaoMedicaoUnica> validadorUnico,
        IValidator<RequisicaoLoteMedicoes> validadorLote)
    {
        _servicoMedicao = servicoMedicao;
        _servicoConsultaMedicao = servicoConsultaMedicao;
        _validadorUnico = validadorUnico;
        _validadorLote = validadorLote;
    }

    [HttpGet]
    public async Task<IActionResult> ListarMedicoes(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        [FromQuery] int? idSensor = null,
        CancellationToken tokenCancelamento = default)
    {
        var resultado = await _servicoConsultaMedicao.ListarPaginadoAsync(
            pagina, tamanhoPagina, idSensor, tokenCancelamento);
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterMedicaoPorId(int id, CancellationToken tokenCancelamento = default)
    {
        var resultado = await _servicoConsultaMedicao.ObterPorIdAsync(id, tokenCancelamento);
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }

    [HttpPost]
    public async Task<IActionResult> PublicarMedicao(
        [FromBody] RequisicaoMedicaoUnica requisicao,
        CancellationToken tokenCancelamento)
    {
        var resultadoValidacao = await _validadorUnico.ValidateAsync(requisicao, tokenCancelamento);
        if (!resultadoValidacao.IsValid)
        {
            var erros = string.Join("; ", resultadoValidacao.Errors.Select(e => e.ErrorMessage));
            return BadRequest(RespostaApi.De(ResultadoOperacao<bool>.Falha(erros)));
        }

        var (sucesso, alertas) = await _servicoMedicao.ProcessarMedicaoAsync(requisicao, tokenCancelamento);
        var resultado = ResultadoOperacao<object>.Ok(new { alertasDisparados = alertas.Count }, "Medição processada com sucesso");
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }

    [HttpPost("lote")]
    public async Task<IActionResult> PublicarMedicoesLote(
        [FromBody] RequisicaoLoteMedicoes requisicao,
        CancellationToken tokenCancelamento)
    {
        var resultadoValidacao = await _validadorLote.ValidateAsync(requisicao, tokenCancelamento);
        if (!resultadoValidacao.IsValid)
        {
            var erros = string.Join("; ", resultadoValidacao.Errors.Select(e => e.ErrorMessage));
            return BadRequest(RespostaApi.De(ResultadoOperacao<bool>.Falha(erros)));
        }

        var (sucesso, alertas) = await _servicoMedicao.ProcessarLoteMedicoesAsync(requisicao, tokenCancelamento);
        var resultado = ResultadoOperacao<object>.Ok(
            new { totalMedicoes = requisicao.Medicoes.Count, alertasDisparados = alertas.Count },
            $"{requisicao.Medicoes.Count} medições processadas com sucesso");
        var resposta = RespostaApi.De(resultado);
        return StatusCode(resposta.Codigo, resposta);
    }
}
