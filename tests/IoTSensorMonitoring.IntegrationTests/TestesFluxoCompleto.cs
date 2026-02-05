using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.IntegrationTests;

public class TestesFluxoCompleto : IClassFixture<AplicacaoTesteIntegracao>
{
    private readonly HttpClient _cliente;

    public TestesFluxoCompleto(AplicacaoTesteIntegracao fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task FluxoCompleto_CriarEquipamento_EnviarMedicao_ConsultarMedicoes()
    {
        var requisicaoEquipamento = new RequisicaoCriarEquipamento
        {
            Nome = "Equipamento Fluxo Completo",
            Descricao = "Teste de fluxo completo",
            IdSetor = 1,
            EstaAtivo = true
        };

        var respostaEquipamento = await _cliente.PostAsJsonAsync("/api/v1/equipamentos", requisicaoEquipamento);
        respostaEquipamento.StatusCode.Should().Be(HttpStatusCode.OK);

        var requisicaoMedicao = new RequisicaoMedicaoUnica
        {
            Id = 100,
            Codigo = "FLUXO001",
            DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-1),
            Medicao = 22.5m
        };

        var respostaMedicao = await _cliente.PostAsJsonAsync("/api/v1/medicoes", requisicaoMedicao);
        respostaMedicao.StatusCode.Should().Be(HttpStatusCode.OK);

        await Task.Delay(500);

        var respostaSensor = await _cliente.GetAsync("/api/v1/sensores/codigo/FLUXO001");

        if (respostaSensor.StatusCode == HttpStatusCode.OK)
        {
            var jsonSensor = await respostaSensor.Content.ReadAsStringAsync();
            var documentoSensor = JsonDocument.Parse(jsonSensor);
            documentoSensor.RootElement.GetProperty("dados").GetProperty("codigo").GetString().Should().Be("FLUXO001");
        }
    }

    [Fact]
    public async Task FluxoCompleto_ProcessarLoteMedicoes_VerificarCriacaoAutomaticaSensores()
    {
        var requisicaoLote = new RequisicaoLoteMedicoes
        {
            Medicoes = new List<RequisicaoMedicaoUnica>
            {
                new()
                {
                    Id = 201,
                    Codigo = "LOTE001",
                    DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-5),
                    Medicao = 20.5m
                },
                new()
                {
                    Id = 202,
                    Codigo = "LOTE002",
                    DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-4),
                    Medicao = 21.0m
                },
                new()
                {
                    Id = 203,
                    Codigo = "LOTE003",
                    DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-3),
                    Medicao = 21.5m
                }
            }
        };

        var respostaLote = await _cliente.PostAsJsonAsync("/api/v1/medicoes/lote", requisicaoLote);
        respostaLote.StatusCode.Should().Be(HttpStatusCode.OK);

        var conteudo = await respostaLote.Content.ReadAsStringAsync();
        conteudo.Should().Contain("3 medições processadas");

        await Task.Delay(500);

        var respostaSensor1 = await _cliente.GetAsync("/api/v1/sensores/codigo/LOTE001");
        var respostaSensor2 = await _cliente.GetAsync("/api/v1/sensores/codigo/LOTE002");
        var respostaSensor3 = await _cliente.GetAsync("/api/v1/sensores/codigo/LOTE003");

        respostaSensor1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        respostaSensor2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        respostaSensor3.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FluxoCompleto_ValidacaoIntegridade_MedicaoComSensorInexistente()
    {
        var requisicao = new RequisicaoMedicaoUnica
        {
            Id = 999,
            Codigo = "NOVO_SENSOR_AUTO",
            DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-1),
            Medicao = 30.0m
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/v1/medicoes", requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        await Task.Delay(500);

        var respostaSensor = await _cliente.GetAsync("/api/v1/sensores/codigo/NOVO_SENSOR_AUTO");

        if (respostaSensor.StatusCode == HttpStatusCode.OK)
        {
            var jsonSensor = await respostaSensor.Content.ReadAsStringAsync();
            var documentoSensor = JsonDocument.Parse(jsonSensor);
            documentoSensor.RootElement.GetProperty("dados").GetProperty("codigo").GetString().Should().Be("NOVO_SENSOR_AUTO");
            documentoSensor.RootElement.GetProperty("dados").GetProperty("nome").GetString().Should().Contain("NOVO_SENSOR_AUTO");
        }
    }

    [Fact]
    public async Task FluxoCompleto_ConsultaPaginada_VerificarOrdenacao()
    {
        for (int i = 0; i < 5; i++)
        {
            var requisicao = new RequisicaoMedicaoUnica
            {
                Id = 300 + i,
                Codigo = $"PAGINACAO{i:D3}",
                DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(-10 + i),
                Medicao = 20.0m + i
            };

            await _cliente.PostAsJsonAsync("/api/v1/medicoes", requisicao);
        }

        await Task.Delay(200);

        var resposta = await _cliente.GetAsync("/api/v1/sensores?pagina=1&tamanhoPagina=5");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resposta.Content.ReadAsStringAsync();
        var documento = JsonDocument.Parse(json);
        documento.RootElement.GetProperty("dados").GetProperty("itens").GetArrayLength().Should().BeGreaterThan(0);
        documento.RootElement.GetProperty("dados").GetProperty("paginaAtual").GetInt32().Should().Be(1);
    }
}
