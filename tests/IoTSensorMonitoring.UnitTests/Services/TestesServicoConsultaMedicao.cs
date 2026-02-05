using FluentAssertions;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace IoTSensorMonitoring.UnitTests.Services;

public class TestesServicoConsultaMedicao
{
    private readonly Mock<IRepositorioMedicao> _mockRepositorioMedicao;
    private readonly Mock<ILogger<ServicoConsultaMedicao>> _mockLogger;
    private readonly ServicoConsultaMedicao _servico;

    public TestesServicoConsultaMedicao()
    {
        _mockRepositorioMedicao = new Mock<IRepositorioMedicao>();
        _mockLogger = new Mock<ILogger<ServicoConsultaMedicao>>();

        _servico = new ServicoConsultaMedicao(
            _mockRepositorioMedicao.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarMedicao_QuandoExistir()
    {
        var idMedicao = 1;
        var medicao = new Medicao
        {
            Id = idMedicao,
            IdSensor = 1,
            ValorMedicao = 25.5m,
            DataHoraMedicao = DateTimeOffset.UtcNow,
            RecebidoEm = DateTimeOffset.UtcNow,
            Sensor = new Sensor { Id = 1, Codigo = "S001", Nome = "Sensor 1" }
        };

        _mockRepositorioMedicao
            .Setup(x => x.ObterPorIdAsync(idMedicao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medicao);

        var resultado = await _servico.ObterPorIdAsync(idMedicao);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados.Should().NotBeNull();
        resultado.Dados!.Id.Should().Be(idMedicao);
        resultado.Dados.ValorMedicao.Should().Be(25.5m);
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarNaoEncontrado_QuandoNaoExistir()
    {
        var idMedicao = 999;

        _mockRepositorioMedicao
            .Setup(x => x.ObterPorIdAsync(idMedicao, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Medicao?)null);

        var resultado = await _servico.ObterPorIdAsync(idMedicao);

        resultado.Sucesso.Should().BeFalse();
        resultado.CodigoHttp.Should().Be(404);
    }

    [Fact]
    public async Task ListarPaginadoAsync_DeveRetornarListaPaginada()
    {
        var medicoes = new List<Medicao>
        {
            new Medicao
            {
                Id = 1,
                IdSensor = 1,
                ValorMedicao = 25.5m,
                DataHoraMedicao = DateTimeOffset.UtcNow,
                RecebidoEm = DateTimeOffset.UtcNow,
                Sensor = new Sensor { Id = 1, Codigo = "S001", Nome = "Sensor 1" }
            },
            new Medicao
            {
                Id = 2,
                IdSensor = 1,
                ValorMedicao = 26.0m,
                DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(1),
                RecebidoEm = DateTimeOffset.UtcNow.AddMinutes(1),
                Sensor = new Sensor { Id = 1, Codigo = "S001", Nome = "Sensor 1" }
            }
        };

        _mockRepositorioMedicao
            .Setup(x => x.ListarPaginadoAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((medicoes, 2));

        var resultado = await _servico.ListarPaginadoAsync(1, 10, null);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados.Should().NotBeNull();
        resultado.Dados!.Itens.Should().HaveCount(2);
        resultado.Dados.TotalItens.Should().Be(2);
        resultado.Dados.PaginaAtual.Should().Be(1);
    }

    [Fact]
    public async Task ListarPaginadoAsync_DeveFiltrarPorSensor_QuandoIdSensorInformado()
    {
        var idSensor = 1;
        var medicoes = new List<Medicao>
        {
            new Medicao
            {
                Id = 1,
                IdSensor = idSensor,
                ValorMedicao = 25.5m,
                DataHoraMedicao = DateTimeOffset.UtcNow,
                RecebidoEm = DateTimeOffset.UtcNow,
                Sensor = new Sensor { Id = idSensor, Codigo = "S001", Nome = "Sensor 1" }
            }
        };

        _mockRepositorioMedicao
            .Setup(x => x.ListarPaginadoAsync(1, 10, idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync((medicoes, 1));

        var resultado = await _servico.ListarPaginadoAsync(1, 10, idSensor);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.Itens.Should().HaveCount(1);
        resultado.Dados.Itens[0].IdSensor.Should().Be(idSensor);

        _mockRepositorioMedicao.Verify(
            x => x.ListarPaginadoAsync(1, 10, idSensor, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListarPaginadoAsync_DeveAjustarPaginaInvalida()
    {
        _mockRepositorioMedicao
            .Setup(x => x.ListarPaginadoAsync(It.IsAny<int>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Medicao>(), 0));

        var resultado = await _servico.ListarPaginadoAsync(0, 10, null); // Página 0 inválida

        resultado.Sucesso.Should().BeTrue();
        // Deve ter ajustado para página 1
        _mockRepositorioMedicao.Verify(
            x => x.ListarPaginadoAsync(1, 10, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListarPaginadoAsync_DeveAjustarTamanhoPaginaInvalido()
    {
        _mockRepositorioMedicao
            .Setup(x => x.ListarPaginadoAsync(It.IsAny<int>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Medicao>(), 0));

        var resultado1 = await _servico.ListarPaginadoAsync(1, 0, null); // Tamanho 0
        var resultado2 = await _servico.ListarPaginadoAsync(1, 200, null); // Tamanho > 100

        resultado1.Sucesso.Should().BeTrue();
        resultado2.Sucesso.Should().BeTrue();

        // Ambos devem ter sido ajustados para 10, então deve ter sido chamado 2 vezes
        _mockRepositorioMedicao.Verify(
            x => x.ListarPaginadoAsync(1, 10, null, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ListarPaginadoAsync_DeveRetornarListaVazia_QuandoNaoHouverDados()
    {
        _mockRepositorioMedicao
            .Setup(x => x.ListarPaginadoAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Medicao>(), 0));

        var resultado = await _servico.ListarPaginadoAsync(1, 10, null);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.Itens.Should().BeEmpty();
        resultado.Dados.TotalItens.Should().Be(0);
    }
}
