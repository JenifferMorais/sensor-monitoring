using FluentAssertions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace IoTSensorMonitoring.UnitTests.Services;

public class TestesServicoVinculoSensorEquipamento
{
    private readonly Mock<IRepositorioSensor> _mockRepositorioSensor;
    private readonly Mock<IRepositorioEquipamento> _mockRepositorioEquipamento;
    private readonly Mock<IRepositorioVinculoSensorEquipamento> _mockRepositorioVinculo;
    private readonly Mock<ILogger<ServicoVinculoSensorEquipamento>> _mockLogger;
    private readonly ServicoVinculoSensorEquipamento _servico;

    public TestesServicoVinculoSensorEquipamento()
    {
        _mockRepositorioSensor = new Mock<IRepositorioSensor>();
        _mockRepositorioEquipamento = new Mock<IRepositorioEquipamento>();
        _mockRepositorioVinculo = new Mock<IRepositorioVinculoSensorEquipamento>();
        _mockLogger = new Mock<ILogger<ServicoVinculoSensorEquipamento>>();

        _servico = new ServicoVinculoSensorEquipamento(
            _mockRepositorioSensor.Object,
            _mockRepositorioEquipamento.Object,
            _mockRepositorioVinculo.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task VincularSensorAoEquipamentoAsync_DeveVincular_QuandoDadosValidos()
    {
        var idSensor = 1;
        var idEquipamento = 1;
        var requisicao = new RequisicaoVincularSensor
        {
            VinculadoPor = "Sistema"
        };

        var sensor = new Sensor { Id = 1, Codigo = "S001", Nome = "Sensor 1", EstaAtivo = true };
        var equipamento = new Equipamento { Id = 1, Nome = "Equipamento 1", EstaAtivo = true };

        _mockRepositorioSensor
            .Setup(x => x.ObterPorIdAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        _mockRepositorioEquipamento
            .Setup(x => x.ObterPorIdAsync(idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipamento);

        _mockRepositorioVinculo
            .Setup(x => x.ObterVinculoAtivoAsync(idSensor, idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VinculoSensorEquipamento?)null);

        var (sucesso, mensagem) = await _servico.VincularSensorAoEquipamentoAsync(idSensor, idEquipamento, requisicao);

        sucesso.Should().BeTrue();
        mensagem.Should().Be("Sensor vinculado com sucesso");

        _mockRepositorioVinculo.Verify(
            x => x.AdicionarAsync(
                It.Is<VinculoSensorEquipamento>(v =>
                    v.IdSensor == idSensor &&
                    v.IdEquipamento == idEquipamento &&
                    v.VinculadoPor == requisicao.VinculadoPor &&
                    v.EstaAtivo),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VincularSensorAoEquipamentoAsync_DeveRetornarErro_QuandoSensorNaoExistir()
    {
        var idSensor = 999;
        var idEquipamento = 1;
        var requisicao = new RequisicaoVincularSensor
        {
            VinculadoPor = "Sistema"
        };

        _mockRepositorioSensor
            .Setup(x => x.ObterPorIdAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sensor?)null);

        var (sucesso, mensagem) = await _servico.VincularSensorAoEquipamentoAsync(idSensor, idEquipamento, requisicao);

        sucesso.Should().BeFalse();
        mensagem.Should().Be("Sensor não encontrado");

        _mockRepositorioVinculo.Verify(
            x => x.AdicionarAsync(It.IsAny<VinculoSensorEquipamento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task VincularSensorAoEquipamentoAsync_DeveRetornarErro_QuandoEquipamentoNaoExistir()
    {
        var idSensor = 1;
        var idEquipamento = 999;
        var requisicao = new RequisicaoVincularSensor
        {
            VinculadoPor = "Sistema"
        };

        var sensor = new Sensor { Id = 1, Codigo = "S001", Nome = "Sensor 1", EstaAtivo = true };

        _mockRepositorioSensor
            .Setup(x => x.ObterPorIdAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        _mockRepositorioEquipamento
            .Setup(x => x.ObterPorIdAsync(idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Equipamento?)null);

        var (sucesso, mensagem) = await _servico.VincularSensorAoEquipamentoAsync(idSensor, idEquipamento, requisicao);

        sucesso.Should().BeFalse();
        mensagem.Should().Be("Equipamento não encontrado");

        _mockRepositorioVinculo.Verify(
            x => x.AdicionarAsync(It.IsAny<VinculoSensorEquipamento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task VincularSensorAoEquipamentoAsync_DeveRetornarErro_QuandoVinculoJaExistir()
    {
        var idSensor = 1;
        var idEquipamento = 1;
        var requisicao = new RequisicaoVincularSensor
        {
            VinculadoPor = "Sistema"
        };

        var sensor = new Sensor { Id = 1, Codigo = "S001", Nome = "Sensor 1", EstaAtivo = true };
        var equipamento = new Equipamento { Id = 1, Nome = "Equipamento 1", EstaAtivo = true };
        var vinculoExistente = new VinculoSensorEquipamento
        {
            IdSensor = 1,
            IdEquipamento = 1,
            EstaAtivo = true
        };

        _mockRepositorioSensor
            .Setup(x => x.ObterPorIdAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        _mockRepositorioEquipamento
            .Setup(x => x.ObterPorIdAsync(idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipamento);

        _mockRepositorioVinculo
            .Setup(x => x.ObterVinculoAtivoAsync(idSensor, idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculoExistente);

        var (sucesso, mensagem) = await _servico.VincularSensorAoEquipamentoAsync(idSensor, idEquipamento, requisicao);

        sucesso.Should().BeFalse();
        mensagem.Should().Be("Sensor já está vinculado a este equipamento");

        _mockRepositorioVinculo.Verify(
            x => x.AdicionarAsync(It.IsAny<VinculoSensorEquipamento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
