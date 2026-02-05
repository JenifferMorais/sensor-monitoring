using FluentAssertions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace IoTSensorMonitoring.UnitTests.Services;

public class TestesServicoMedicao
{
    private readonly Mock<IRepositorioMedicao> _mockRepositorioMedicao;
    private readonly Mock<IRepositorioSensor> _mockRepositorioSensor;
    private readonly Mock<IRepositorioEquipamento> _mockRepositorioEquipamento;
    private readonly Mock<IPublicadorMensagens> _mockPublicador;
    private readonly Mock<ILogger<ServicoMedicao>> _mockLogger;
    private readonly ServicoAvaliacaoAlerta _avaliadorAlerta;
    private readonly ServicoMedicao _servico;

    public TestesServicoMedicao()
    {
        _mockRepositorioMedicao = new Mock<IRepositorioMedicao>();
        _mockRepositorioSensor = new Mock<IRepositorioSensor>();
        _mockRepositorioEquipamento = new Mock<IRepositorioEquipamento>();
        _mockPublicador = new Mock<IPublicadorMensagens>();
        _mockLogger = new Mock<ILogger<ServicoMedicao>>();

        var mockRepositorioRegraAlerta = new Mock<IRepositorioRegraAlerta>();
        var mockRepositorioEstadoAlerta = new Mock<IRepositorioEstadoAlerta>();
        var mockRepositorioHistoricoAlerta = new Mock<IRepositorioHistoricoAlerta>();
        var mockRepositorioMedicaoParaAlerta = new Mock<IRepositorioMedicao>();
        var mockLoggerAlerta = new Mock<ILogger<ServicoAvaliacaoAlerta>>();

        _avaliadorAlerta = new ServicoAvaliacaoAlerta(
            mockRepositorioRegraAlerta.Object,
            mockRepositorioEstadoAlerta.Object,
            mockRepositorioHistoricoAlerta.Object,
            mockLoggerAlerta.Object);

        _servico = new ServicoMedicao(
            _mockRepositorioMedicao.Object,
            _mockRepositorioSensor.Object,
            _mockRepositorioEquipamento.Object,
            _avaliadorAlerta,
            _mockLogger.Object,
            _mockPublicador.Object);
    }

    [Fact]
    public async Task ProcessarMedicaoAsync_DeveSalvarMedicao_ComSensorExistente()
    {
        var requisicao = new RequisicaoMedicaoUnica
        {
            Id = 1,
            Codigo = "TEMP001",
            DataHoraMedicao = DateTimeOffset.UtcNow,
            Medicao = 25.5m
        };

        var sensor = new Sensor
        {
            Id = 1,
            Codigo = "TEMP001",
            Nome = "Sensor de Temperatura 1",
            EstaAtivo = true
        };

        _mockRepositorioSensor
            .Setup(x => x.ObterPorIdAsync(requisicao.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        var (sucesso, alertas) = await _servico.ProcessarMedicaoAsync(requisicao);

        sucesso.Should().BeTrue();
        alertas.Should().BeEmpty(); // Com publicador configurado, não avalia alertas

        _mockRepositorioMedicao.Verify(
            x => x.AdicionarAsync(
                It.Is<Medicao>(m =>
                    m.IdSensor == sensor.Id &&
                    m.ValorMedicao == requisicao.Medicao &&
                    m.DataHoraMedicao == requisicao.DataHoraMedicao),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockPublicador.Verify(
            x => x.Publicar(It.Is<string>(s => s.Contains(sensor.Codigo)), It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMedicaoAsync_DeveCriarNovoSensor_QuandoNaoExistir()
    {
        var requisicao = new RequisicaoMedicaoUnica
        {
            Id = 999,
            Codigo = "NEW001",
            DataHoraMedicao = DateTimeOffset.UtcNow,
            Medicao = 30.0m
        };

        _mockRepositorioSensor
            .Setup(x => x.ObterPorIdAsync(requisicao.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sensor?)null);

        _mockRepositorioSensor
            .Setup(x => x.ObterPorCodigoAsync(requisicao.Codigo, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sensor?)null);

        _mockRepositorioSensor
            .Setup(x => x.AdicionarAsync(It.IsAny<Sensor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sensor s, CancellationToken ct) => s);

        var (sucesso, alertas) = await _servico.ProcessarMedicaoAsync(requisicao);

        sucesso.Should().BeTrue();

        _mockRepositorioSensor.Verify(
            x => x.AdicionarAsync(
                It.Is<Sensor>(s =>
                    s.Id == requisicao.Id &&
                    s.Codigo == requisicao.Codigo &&
                    s.Nome == $"Sensor {requisicao.Codigo}" &&
                    s.EstaAtivo),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMedicaoAsync_DeveAvaliarAlertas_QuandoPublicadorNulo()
    {
        var requisicao = new RequisicaoMedicaoUnica
        {
            Id = 1,
            Codigo = "TEMP001",
            DataHoraMedicao = DateTimeOffset.UtcNow,
            Medicao = 100.0m // Valor alto que pode disparar alerta
        };

        var sensor = new Sensor
        {
            Id = 1,
            Codigo = "TEMP001",
            Nome = "Sensor de Temperatura 1",
            EstaAtivo = true
        };

        _mockRepositorioSensor
            .Setup(x => x.ObterPorIdAsync(requisicao.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        // Criar um mock de repositório de regras de alerta que retorna uma lista vazia
        // Isso faz com que o avaliador não dispare alertas
        var mockRepositorioRegraAlerta = new Mock<IRepositorioRegraAlerta>();
        mockRepositorioRegraAlerta
            .Setup(x => x.ObterAtivasPorIdSensorAsync(sensor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RegraAlerta>());

        var mockRepositorioEstadoAlerta = new Mock<IRepositorioEstadoAlerta>();
        var mockRepositorioHistoricoAlerta = new Mock<IRepositorioHistoricoAlerta>();
        var mockLoggerAlerta = new Mock<ILogger<ServicoAvaliacaoAlerta>>();

        var avaliadorAlerta = new ServicoAvaliacaoAlerta(
            mockRepositorioRegraAlerta.Object,
            mockRepositorioEstadoAlerta.Object,
            mockRepositorioHistoricoAlerta.Object,
            mockLoggerAlerta.Object);

        // Criar serviço sem publicador
        var servicoSemPublicador = new ServicoMedicao(
            _mockRepositorioMedicao.Object,
            _mockRepositorioSensor.Object,
            _mockRepositorioEquipamento.Object,
            avaliadorAlerta,
            _mockLogger.Object,
            null); // Sem publicador

        var (sucesso, alertas) = await servicoSemPublicador.ProcessarMedicaoAsync(requisicao);

        sucesso.Should().BeTrue();
        alertas.Should().BeEmpty(); // Sem regras de alerta, não deve disparar alertas

        // Verificar que o repositório de regras foi consultado
        mockRepositorioRegraAlerta.Verify(
            x => x.ObterAtivasPorIdSensorAsync(sensor.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarLoteMedicoesAsync_DeveProcessarTodasMedicoes()
    {
        var requisicao = new RequisicaoLoteMedicoes
        {
            Medicoes = new List<RequisicaoMedicaoUnica>
            {
                new RequisicaoMedicaoUnica
                {
                    Id = 1,
                    Codigo = "TEMP001",
                    DataHoraMedicao = DateTimeOffset.UtcNow,
                    Medicao = 25.5m
                },
                new RequisicaoMedicaoUnica
                {
                    Id = 2,
                    Codigo = "TEMP002",
                    DataHoraMedicao = DateTimeOffset.UtcNow,
                    Medicao = 26.0m
                }
            }
        };

        var sensor1 = new Sensor { Id = 1, Codigo = "TEMP001", Nome = "Sensor 1", EstaAtivo = true };
        var sensor2 = new Sensor { Id = 2, Codigo = "TEMP002", Nome = "Sensor 2", EstaAtivo = true };

        _mockRepositorioSensor
            .Setup(x => x.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor1);

        _mockRepositorioSensor
            .Setup(x => x.ObterPorIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor2);

        var (sucesso, alertas) = await _servico.ProcessarLoteMedicoesAsync(requisicao);

        sucesso.Should().BeTrue();

        _mockRepositorioMedicao.Verify(
            x => x.AdicionarLoteAsync(
                It.Is<List<Medicao>>(list => list.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockPublicador.Verify(
            x => x.Publicar(It.IsAny<string>(), It.IsAny<object>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessarLoteMedicoesAsync_DeveCriarNovosSensores_QuandoNecessario()
    {
        var requisicao = new RequisicaoLoteMedicoes
        {
            Medicoes = new List<RequisicaoMedicaoUnica>
            {
                new RequisicaoMedicaoUnica
                {
                    Id = 999,
                    Codigo = "NEW001",
                    DataHoraMedicao = DateTimeOffset.UtcNow,
                    Medicao = 25.5m
                }
            }
        };

        _mockRepositorioSensor
            .Setup(x => x.ObterPorIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sensor?)null);

        _mockRepositorioSensor
            .Setup(x => x.ObterPorCodigoAsync("NEW001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sensor?)null);

        var (sucesso, alertas) = await _servico.ProcessarLoteMedicoesAsync(requisicao);

        sucesso.Should().BeTrue();

        _mockRepositorioSensor.Verify(
            x => x.AdicionarAsync(
                It.Is<Sensor>(s => s.Codigo == "NEW001"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarLoteMedicoesAsync_DeveUsarCache_ParaSensoresDuplicados()
    {
        var requisicao = new RequisicaoLoteMedicoes
        {
            Medicoes = new List<RequisicaoMedicaoUnica>
            {
                new RequisicaoMedicaoUnica
                {
                    Id = 1,
                    Codigo = "TEMP001",
                    DataHoraMedicao = DateTimeOffset.UtcNow,
                    Medicao = 25.5m
                },
                new RequisicaoMedicaoUnica
                {
                    Id = 1,
                    Codigo = "TEMP001",
                    DataHoraMedicao = DateTimeOffset.UtcNow.AddMinutes(1),
                    Medicao = 26.0m
                }
            }
        };

        var sensor = new Sensor { Id = 1, Codigo = "TEMP001", Nome = "Sensor 1", EstaAtivo = true };

        _mockRepositorioSensor
            .Setup(x => x.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        var (sucesso, alertas) = await _servico.ProcessarLoteMedicoesAsync(requisicao);

        sucesso.Should().BeTrue();

        // Deve buscar o sensor apenas uma vez (cache funciona)
        _mockRepositorioSensor.Verify(
            x => x.ObterPorIdAsync(1, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRepositorioMedicao.Verify(
            x => x.AdicionarLoteAsync(
                It.Is<List<Medicao>>(list => list.Count == 2 && list.All(m => m.IdSensor == 1)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ObterMedicoesEquipamentoAsync_DeveRetornarMedicoes_QuandoEquipamentoExistir()
    {
        var idEquipamento = 1;
        var equipamento = new Equipamento
        {
            Id = idEquipamento,
            Nome = "Equipamento 1",
            EstaAtivo = true,
            VinculosSensorEquipamento = new List<VinculoSensorEquipamento>
            {
                new VinculoSensorEquipamento
                {
                    IdSensor = 1,
                    IdEquipamento = idEquipamento,
                    Sensor = new Sensor { Id = 1, Codigo = "TEMP001", Nome = "Sensor 1" }
                }
            }
        };

        var medicoes = new List<Medicao>
        {
            new Medicao
            {
                Id = 1,
                IdSensor = 1,
                ValorMedicao = 25.5m,
                DataHoraMedicao = DateTimeOffset.UtcNow,
                Sensor = new Sensor { Id = 1, Codigo = "TEMP001", Nome = "Sensor 1" }
            }
        };

        _mockRepositorioEquipamento
            .Setup(x => x.ObterPorIdComSensoresAsync(idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipamento);

        _mockRepositorioMedicao
            .Setup(x => x.ObterUltimasPorEquipamentoAsync(idEquipamento, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medicoes);

        var resultado = await _servico.ObterMedicoesEquipamentoAsync(idEquipamento);

        resultado.Should().NotBeNull();
        resultado!.IdEquipamento.Should().Be(idEquipamento);
        resultado.NomeEquipamento.Should().Be("Equipamento 1");
    }

    [Fact]
    public async Task ObterMedicoesEquipamentoAsync_DeveRetornarNulo_QuandoEquipamentoNaoExistir()
    {
        var idEquipamento = 999;

        _mockRepositorioEquipamento
            .Setup(x => x.ObterPorIdComSensoresAsync(idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Equipamento?)null);

        var resultado = await _servico.ObterMedicoesEquipamentoAsync(idEquipamento);

        resultado.Should().BeNull();

        _mockRepositorioMedicao.Verify(
            x => x.ObterUltimasPorEquipamentoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
