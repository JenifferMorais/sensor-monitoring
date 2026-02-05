using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.IntegrationTests;

public class TestesApiEquipamentos : IClassFixture<AplicacaoTesteIntegracao>
{
    private readonly HttpClient _cliente;

    public TestesApiEquipamentos(AplicacaoTesteIntegracao fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task GetEquipamentos_DeveRetornarListaPaginada()
    {
        var resposta = await _cliente.GetAsync("/api/v1/equipamentos?pagina=1&tamanhoPagina=10");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resposta.Content.ReadAsStringAsync();
        var documento = JsonDocument.Parse(json);
        documento.RootElement.GetProperty("sucesso").GetBoolean().Should().BeTrue();
        documento.RootElement.GetProperty("dados").GetProperty("itens").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetEquipamentoPorId_QuandoExiste_DeveRetornarEquipamento()
    {
        var resposta = await _cliente.GetAsync("/api/v1/equipamentos/1");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resposta.Content.ReadAsStringAsync();
        var documento = JsonDocument.Parse(json);
        documento.RootElement.GetProperty("sucesso").GetBoolean().Should().BeTrue();
        documento.RootElement.GetProperty("dados").GetProperty("id").GetInt32().Should().Be(1);
        documento.RootElement.GetProperty("dados").GetProperty("nome").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetEquipamentoPorId_QuandoNaoExiste_DeveRetornarNotFound()
    {
        var resposta = await _cliente.GetAsync("/api/v1/equipamentos/999");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostEquipamento_ComDadosValidos_DeveCriarEquipamento()
    {
        var requisicao = new RequisicaoCriarEquipamento
        {
            Nome = "Novo Equipamento",
            Descricao = "Descrição do novo equipamento",
            IdSetor = 1,
            EstaAtivo = true
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/equipamentos", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resposta.Content.ReadAsStringAsync();
        var documento = JsonDocument.Parse(json);
        documento.RootElement.GetProperty("sucesso").GetBoolean().Should().BeTrue();
        documento.RootElement.GetProperty("dados").GetProperty("nome").GetString().Should().Be("Novo Equipamento");
        documento.RootElement.GetProperty("dados").GetProperty("id").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PostEquipamento_SemNome_DeveRetornarBadRequest()
    {
        var requisicao = new RequisicaoCriarEquipamento
        {
            Nome = "",
            IdSetor = 1
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/equipamentos", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutEquipamento_ComDadosValidos_DeveAtualizar()
    {
        var requisicao = new RequisicaoAtualizarEquipamento
        {
            Nome = "Equipamento Atualizado",
            Descricao = "Descrição atualizada",
            IdSetor = 1,
            EstaAtivo = true
        };

        var resposta = await _cliente.PutAsJsonAsync("/api/v1/equipamentos/1", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resposta.Content.ReadAsStringAsync();
        var documento = JsonDocument.Parse(json);
        documento.RootElement.GetProperty("sucesso").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DeleteEquipamento_QuandoExiste_DeveRemover()
    {
        var criarRequisicao = new RequisicaoCriarEquipamento
        {
            Nome = "Equipamento Para Deletar",
            IdSetor = 1
        };

        var respostaCriar = await _cliente.PostAsJsonAsync("/api/v1/equipamentos", criarRequisicao);
        var jsonCriar = await respostaCriar.Content.ReadAsStringAsync();
        var documentoCriar = JsonDocument.Parse(jsonCriar);
        var idEquipamento = documentoCriar.RootElement.GetProperty("dados").GetProperty("id").GetInt32();

        var respostaDeletar = await _cliente.DeleteAsync($"/api/v1/equipamentos/{idEquipamento}");

        respostaDeletar.StatusCode.Should().Be(HttpStatusCode.OK);

        var respostaGet = await _cliente.GetAsync($"/api/v1/equipamentos/{idEquipamento}");
        respostaGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
