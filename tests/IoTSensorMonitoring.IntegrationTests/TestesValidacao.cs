using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.IntegrationTests;

public class TestesValidacao : IClassFixture<AplicacaoTesteIntegracao>
{
    private readonly HttpClient _cliente;

    public TestesValidacao(AplicacaoTesteIntegracao fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task PostMedicao_ComCodigoVazio_DeveRetornarBadRequest()
    {
        var requisicao = new RequisicaoMedicaoUnica
        {
            Id = 1,
            Codigo = "",
            DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-1),
            Medicao = 25.0m
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/medicoes", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMedicao_ComValorAbaixoDoMinimo_DeveRetornarBadRequest()
    {
        var requisicao = new RequisicaoMedicaoUnica
        {
            Id = 1,
            Codigo = "TESTE001",
            DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-1),
            Medicao = -300m
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/medicoes", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMedicao_ComValorAcimaDoMaximo_DeveRetornarBadRequest()
    {
        var requisicao = new RequisicaoMedicaoUnica
        {
            Id = 1,
            Codigo = "TESTE001",
            DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-1),
            Medicao = 1500m
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/medicoes", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMedicao_ComDataFutura_DeveRetornarBadRequest()
    {
        var requisicao = new RequisicaoMedicaoUnica
        {
            Id = 1,
            Codigo = "TESTE001",
            DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(10),
            Medicao = 25.0m
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/medicoes", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostLoteMedicoes_ComMedicaoInvalida_DeveRetornarBadRequest()
    {
        var requisicao = new RequisicaoLoteMedicoes
        {
            Medicoes = new List<RequisicaoMedicaoUnica>
            {
                new()
                {
                    Id = 1,
                    Codigo = "TESTE001",
                    DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-1),
                    Medicao = 25.0m
                },
                new()
                {
                    Id = 2,
                    Codigo = "",
                    DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-1),
                    Medicao = 26.0m
                }
            }
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/medicoes/lote", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostEquipamento_ComNomeMuitoLongo_DeveRetornarBadRequest()
    {
        var nomeGrande = new string('A', 300);

        var requisicao = new RequisicaoCriarEquipamento
        {
            Nome = nomeGrande,
            IdSetor = 1
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/equipamentos", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutEquipamento_ComIdInexistente_DeveRetornarNotFound()
    {
        var requisicao = new RequisicaoAtualizarEquipamento
        {
            Nome = "Equipamento Atualizado",
            IdSetor = 1,
            EstaAtivo = true
        };

        var resposta = await _cliente.PutAsJsonAsync("/api/v1/equipamentos/999", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEquipamento_ComIdInexistente_DeveRetornarNotFound()
    {
        var resposta = await _cliente.DeleteAsync("/api/v1/equipamentos/999");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEquipamentos_ComPaginaInvalida_DeveAjustarParaPaginaValida()
    {
        var resposta = await _cliente.GetAsync("/api/v1/equipamentos?pagina=0&tamanhoPagina=10");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resposta.Content.ReadAsStringAsync();
        var documento = JsonDocument.Parse(json);
        documento.RootElement.GetProperty("dados").GetProperty("paginaAtual").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetEquipamentos_ComTamanhoPaginaInvalido_DeveAjustarParaTamanhoValido()
    {
        var resposta = await _cliente.GetAsync("/api/v1/equipamentos?pagina=1&tamanhoPagina=500");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resposta.Content.ReadAsStringAsync();
        var documento = JsonDocument.Parse(json);
        documento.RootElement.GetProperty("dados").GetProperty("tamanhoPagina").GetInt32().Should().BeLessThanOrEqualTo(100);
    }
}
