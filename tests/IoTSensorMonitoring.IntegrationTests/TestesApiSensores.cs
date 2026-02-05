using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace IoTSensorMonitoring.IntegrationTests;

public class TestesApiSensores : IClassFixture<AplicacaoTesteIntegracao>
{
    private readonly HttpClient _cliente;

    public TestesApiSensores(AplicacaoTesteIntegracao fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task GetSensores_DeveRetornarListaPaginada()
    {
        var resposta = await _cliente.GetAsync("/api/v1/sensores?pagina=1&tamanhoPagina=10");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resposta.Content.ReadAsStringAsync();
        var documento = JsonDocument.Parse(json);
        documento.RootElement.GetProperty("sucesso").GetBoolean().Should().BeTrue();
        documento.RootElement.GetProperty("dados").GetProperty("itens").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSensorPorId_QuandoExiste_DeveRetornarSensor()
    {
        var resposta = await _cliente.GetAsync("/api/v1/sensores/1");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resposta.Content.ReadAsStringAsync();
        var documento = JsonDocument.Parse(json);
        documento.RootElement.GetProperty("sucesso").GetBoolean().Should().BeTrue();
        documento.RootElement.GetProperty("dados").GetProperty("id").GetInt32().Should().Be(1);
        documento.RootElement.GetProperty("dados").GetProperty("codigo").GetString().Should().Be("SENSOR001");
    }

    [Fact]
    public async Task GetSensorPorId_QuandoNaoExiste_DeveRetornarNotFound()
    {
        var resposta = await _cliente.GetAsync("/api/v1/sensores/999");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSensorPorCodigo_QuandoExiste_DeveRetornarSensor()
    {
        var resposta = await _cliente.GetAsync("/api/v1/sensores/codigo/SENSOR001");

        resposta.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (resposta.StatusCode == HttpStatusCode.OK)
        {
            var json = await resposta.Content.ReadAsStringAsync();
            var documento = JsonDocument.Parse(json);
            documento.RootElement.GetProperty("sucesso").GetBoolean().Should().BeTrue();
            documento.RootElement.GetProperty("dados").GetProperty("codigo").GetString().Should().Be("SENSOR001");
        }
    }

    [Fact]
    public async Task GetSensorPorCodigo_QuandoNaoExiste_DeveRetornarNotFound()
    {
        var resposta = await _cliente.GetAsync("/api/v1/sensores/codigo/INEXISTENTE");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
