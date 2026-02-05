using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IoTSensorMonitoring.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IoTSensorMonitoring.IntegrationTests;

public class TestesApiMedicoes : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _cliente;

    public TestesApiMedicoes(WebApplicationFactory<Program> fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task PostMedicao_RequisicaoValida_RetornaOk()
    {
        var requisicao = new RequisicaoMedicaoUnica
        {
            Id = 1,
            Codigo = "TESTE001",
            DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-5),
            Medicao = 25.5m
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/medicoes", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var conteudo = await resposta.Content.ReadAsStringAsync();
        conteudo.Should().Contain("sucesso");
    }

    [Fact]
    public async Task PostMedicao_MedicaoFutura_RetornaBadRequest()
    {
        var requisicao = new RequisicaoMedicaoUnica
        {
            Id = 1,
            Codigo = "TESTE001",
            DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(5),
            Medicao = 25.5m
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/medicoes", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMedicao_IntervaloInvalido_RetornaBadRequest()
    {
        var requisicao = new RequisicaoMedicaoUnica
        {
            Id = 1,
            Codigo = "TESTE001",
            DataHoraMedicao = DateTimeOffset.UtcNow,
            Medicao = -300m
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/medicoes", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostLoteMedicoes_RequisicaoValida_RetornaOk()
    {
        var requisicao = new RequisicaoLoteMedicoes
        {
            Medicoes = new List<RequisicaoMedicaoUnica>
            {
                new()
                {
                    Id = 1,
                    Codigo = "TESTE001",
                    DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-5),
                    Medicao = 25.5m
                },
                new()
                {
                    Id = 2,
                    Codigo = "TESTE002",
                    DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-4),
                    Medicao = 30.2m
                }
            }
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/medicoes/lote", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var conteudo = await resposta.Content.ReadAsStringAsync();
        conteudo.Should().Contain("2 medições processadas");
    }

    [Fact]
    public async Task PostLoteMedicoes_ListaVazia_RetornaBadRequest()
    {
        var requisicao = new RequisicaoLoteMedicoes
        {
            Medicoes = new List<RequisicaoMedicaoUnica>()
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/medicoes/lote", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
