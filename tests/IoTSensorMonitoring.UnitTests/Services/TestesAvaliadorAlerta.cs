using FluentAssertions;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace IoTSensorMonitoring.UnitTests.Services;

public class TestesServicoAvaliacaoAlerta
{
    private readonly Mock<IRepositorioRegraAlerta> _mockRepositorioRegraAlerta;
    private readonly Mock<IRepositorioEstadoAlerta> _mockRepositorioEstadoAlerta;
    private readonly Mock<IRepositorioHistoricoAlerta> _mockRepositorioHistoricoAlerta;
    private readonly Mock<ILogger<ServicoAvaliacaoAlerta>> _mockLogger;
    private readonly ServicoAvaliacaoAlerta _avaliadorAlerta;

    public TestesServicoAvaliacaoAlerta()
    {
        _mockRepositorioRegraAlerta = new Mock<IRepositorioRegraAlerta>();
        _mockRepositorioEstadoAlerta = new Mock<IRepositorioEstadoAlerta>();
        _mockRepositorioHistoricoAlerta = new Mock<IRepositorioHistoricoAlerta>();
        _mockLogger = new Mock<ILogger<ServicoAvaliacaoAlerta>>();

        _avaliadorAlerta = new ServicoAvaliacaoAlerta(
            _mockRepositorioRegraAlerta.Object,
            _mockRepositorioEstadoAlerta.Object,
            _mockRepositorioHistoricoAlerta.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ConsecutivoForaIntervalo_DeveDispararAlerta_Quando5ValoresBaixosConsecutivos()
    {
        var idSensor = 1;
        var regra = new RegraAlerta
        {
            Id = 1,
            IdSensor = idSensor,
            TipoRegra = TipoRegraAlerta.ConsecutivoForaIntervalo,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            ContagemConsecutiva = 5,
            EmailNotificacao = "teste@exemplo.com",
            EstaAtivo = true
        };

        _mockRepositorioRegraAlerta
            .Setup(x => x.ObterAtivasPorIdSensorAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RegraAlerta> { regra });

        var estado = new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra.Id,
            ContagemConsecutiva = 0,
            JsonMedicoesRecentes = "[]"
        };

        _mockRepositorioEstadoAlerta
            .Setup(x => x.ObterPorRegraAlertaAsync(idSensor, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        for (int i = 1; i <= 4; i++)
        {
            var resultado = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 0.5m, DateTimeOffset.UtcNow);
            resultado.Should().BeEmpty();
            estado.ContagemConsecutiva = i;
        }

        var resultadoFinal = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 0.5m, DateTimeOffset.UtcNow);

        resultadoFinal.Should().HaveCount(1);
        resultadoFinal[0].TipoRegra.Should().Be(TipoRegraAlerta.ConsecutivoForaIntervalo);
        resultadoFinal[0].IdSensor.Should().Be(idSensor);

        _mockRepositorioHistoricoAlerta.Verify(
            x => x.AdicionarAsync(It.IsAny<HistoricoAlerta>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConsecutivoForaIntervalo_DeveDispararAlerta_Quando5ValoresAltosConsecutivos()
    {
        var idSensor = 1;
        var regra = new RegraAlerta
        {
            Id = 1,
            IdSensor = idSensor,
            TipoRegra = TipoRegraAlerta.ConsecutivoForaIntervalo,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            ContagemConsecutiva = 5,
            EmailNotificacao = "teste@exemplo.com",
            EstaAtivo = true
        };

        _mockRepositorioRegraAlerta
            .Setup(x => x.ObterAtivasPorIdSensorAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RegraAlerta> { regra });

        var estado = new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra.Id,
            ContagemConsecutiva = 0,
            JsonMedicoesRecentes = "[]"
        };

        _mockRepositorioEstadoAlerta
            .Setup(x => x.ObterPorRegraAlertaAsync(idSensor, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        for (int i = 1; i <= 4; i++)
        {
            var resultado = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 55m, DateTimeOffset.UtcNow);
            resultado.Should().BeEmpty();
            estado.ContagemConsecutiva = i;
        }

        var resultadoFinal = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 55m, DateTimeOffset.UtcNow);

        resultadoFinal.Should().HaveCount(1);
        resultadoFinal[0].TipoRegra.Should().Be(TipoRegraAlerta.ConsecutivoForaIntervalo);

        _mockRepositorioHistoricoAlerta.Verify(
            x => x.AdicionarAsync(It.IsAny<HistoricoAlerta>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConsecutivoForaIntervalo_DeveResetarContador_QuandoValorVoltaAoNormal()
    {
        var idSensor = 1;
        var regra = new RegraAlerta
        {
            Id = 1,
            IdSensor = idSensor,
            TipoRegra = TipoRegraAlerta.ConsecutivoForaIntervalo,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            ContagemConsecutiva = 5,
            EmailNotificacao = "teste@exemplo.com",
            EstaAtivo = true
        };

        _mockRepositorioRegraAlerta
            .Setup(x => x.ObterAtivasPorIdSensorAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RegraAlerta> { regra });

        var estado = new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra.Id,
            ContagemConsecutiva = 3,
            JsonMedicoesRecentes = "[]"
        };

        _mockRepositorioEstadoAlerta
            .Setup(x => x.ObterPorRegraAlertaAsync(idSensor, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        var resultado = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 25m, DateTimeOffset.UtcNow);

        resultado.Should().BeEmpty();

        _mockRepositorioEstadoAlerta.Verify(
            x => x.InserirOuAtualizarAsync(It.Is<EstadoAlerta>(s => s.ContagemConsecutiva == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConsecutivoForaIntervalo_NaoDeveDisparar_ComApenas4Consecutivos()
    {
        var idSensor = 1;
        var regra = new RegraAlerta
        {
            Id = 1,
            IdSensor = idSensor,
            TipoRegra = TipoRegraAlerta.ConsecutivoForaIntervalo,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            ContagemConsecutiva = 5,
            EmailNotificacao = "teste@exemplo.com",
            EstaAtivo = true
        };

        _mockRepositorioRegraAlerta
            .Setup(x => x.ObterAtivasPorIdSensorAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RegraAlerta> { regra });

        var estado = new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra.Id,
            ContagemConsecutiva = 0,
            JsonMedicoesRecentes = "[]"
        };

        _mockRepositorioEstadoAlerta
            .Setup(x => x.ObterPorRegraAlertaAsync(idSensor, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        for (int i = 1; i <= 4; i++)
        {
            var resultado = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 0.5m, DateTimeOffset.UtcNow);
            resultado.Should().BeEmpty();
            estado.ContagemConsecutiva = i;
        }

        _mockRepositorioHistoricoAlerta.Verify(
            x => x.AdicionarAsync(It.IsAny<HistoricoAlerta>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MediaMargemErro_DeveDispararAlerta_QuandoMediaNaMargemInferior()
    {
        var idSensor = 1;
        var regra = new RegraAlerta
        {
            Id = 1,
            IdSensor = idSensor,
            TipoRegra = TipoRegraAlerta.MediaMargemErro,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            TamanhoJanelaMedia = 50,
            MargemErro = 2,
            EmailNotificacao = "teste@exemplo.com",
            EstaAtivo = true
        };

        _mockRepositorioRegraAlerta
            .Setup(x => x.ObterAtivasPorIdSensorAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RegraAlerta> { regra });

        var medicoes = new List<decimal>();
        for (int i = 0; i < 50; i++)
        {
            medicoes.Add(0.5m);
        }

        var estado = new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra.Id,
            ContagemConsecutiva = 0,
            JsonMedicoesRecentes = System.Text.Json.JsonSerializer.Serialize(medicoes.Take(49).ToList())
        };

        _mockRepositorioEstadoAlerta
            .Setup(x => x.ObterPorRegraAlertaAsync(idSensor, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        var resultado = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 0.5m, DateTimeOffset.UtcNow);

        resultado.Should().HaveCount(1);
        resultado[0].TipoRegra.Should().Be(TipoRegraAlerta.MediaMargemErro);
        resultado[0].ValorDisparo.Should().Be(0.5m);

        _mockRepositorioHistoricoAlerta.Verify(
            x => x.AdicionarAsync(It.IsAny<HistoricoAlerta>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MediaMargemErro_DeveDispararAlerta_QuandoMediaNaMargemSuperior()
    {
        var idSensor = 1;
        var regra = new RegraAlerta
        {
            Id = 1,
            IdSensor = idSensor,
            TipoRegra = TipoRegraAlerta.MediaMargemErro,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            TamanhoJanelaMedia = 50,
            MargemErro = 2,
            EmailNotificacao = "teste@exemplo.com",
            EstaAtivo = true
        };

        _mockRepositorioRegraAlerta
            .Setup(x => x.ObterAtivasPorIdSensorAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RegraAlerta> { regra });

        var medicoes = new List<decimal>();
        for (int i = 0; i < 50; i++)
        {
            medicoes.Add(49.5m);
        }

        var estado = new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra.Id,
            ContagemConsecutiva = 0,
            JsonMedicoesRecentes = System.Text.Json.JsonSerializer.Serialize(medicoes.Take(49).ToList())
        };

        _mockRepositorioEstadoAlerta
            .Setup(x => x.ObterPorRegraAlertaAsync(idSensor, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        var resultado = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 49.5m, DateTimeOffset.UtcNow);

        resultado.Should().HaveCount(1);
        resultado[0].TipoRegra.Should().Be(TipoRegraAlerta.MediaMargemErro);
        resultado[0].ValorDisparo.Should().Be(49.5m);

        _mockRepositorioHistoricoAlerta.Verify(
            x => x.AdicionarAsync(It.IsAny<HistoricoAlerta>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MediaMargemErro_NaoDeveDisparar_QuandoMediaNaFaixaSegura()
    {
        var idSensor = 1;
        var regra = new RegraAlerta
        {
            Id = 1,
            IdSensor = idSensor,
            TipoRegra = TipoRegraAlerta.MediaMargemErro,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            TamanhoJanelaMedia = 50,
            MargemErro = 2,
            EmailNotificacao = "teste@exemplo.com",
            EstaAtivo = true
        };

        _mockRepositorioRegraAlerta
            .Setup(x => x.ObterAtivasPorIdSensorAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RegraAlerta> { regra });

        var medicoes = new List<decimal>();
        for (int i = 0; i < 50; i++)
        {
            medicoes.Add(25m);
        }

        var estado = new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra.Id,
            ContagemConsecutiva = 0,
            JsonMedicoesRecentes = System.Text.Json.JsonSerializer.Serialize(medicoes.Take(49).ToList())
        };

        _mockRepositorioEstadoAlerta
            .Setup(x => x.ObterPorRegraAlertaAsync(idSensor, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        var resultado = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 25m, DateTimeOffset.UtcNow);

        resultado.Should().BeEmpty();

        _mockRepositorioHistoricoAlerta.Verify(
            x => x.AdicionarAsync(It.IsAny<HistoricoAlerta>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MediaMargemErro_NaoDeveDisparar_ComMedicoesInsuficientes()
    {
        var idSensor = 1;
        var regra = new RegraAlerta
        {
            Id = 1,
            IdSensor = idSensor,
            TipoRegra = TipoRegraAlerta.MediaMargemErro,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            TamanhoJanelaMedia = 50,
            MargemErro = 2,
            EmailNotificacao = "teste@exemplo.com",
            EstaAtivo = true
        };

        _mockRepositorioRegraAlerta
            .Setup(x => x.ObterAtivasPorIdSensorAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RegraAlerta> { regra });

        var medicoes = new List<decimal>();
        for (int i = 0; i < 30; i++)
        {
            medicoes.Add(0.5m);
        }

        var estado = new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra.Id,
            ContagemConsecutiva = 0,
            JsonMedicoesRecentes = System.Text.Json.JsonSerializer.Serialize(medicoes)
        };

        _mockRepositorioEstadoAlerta
            .Setup(x => x.ObterPorRegraAlertaAsync(idSensor, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        var resultado = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 0.5m, DateTimeOffset.UtcNow);

        resultado.Should().BeEmpty();

        _mockRepositorioHistoricoAlerta.Verify(
            x => x.AdicionarAsync(It.IsAny<HistoricoAlerta>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegraMultipla_DeveDispararAmbas_QuandoCondicoesAtendidas()
    {
        var idSensor = 1;
        var regra1 = new RegraAlerta
        {
            Id = 1,
            IdSensor = idSensor,
            TipoRegra = TipoRegraAlerta.ConsecutivoForaIntervalo,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            ContagemConsecutiva = 5,
            EmailNotificacao = "teste@exemplo.com",
            EstaAtivo = true
        };

        var regra2 = new RegraAlerta
        {
            Id = 2,
            IdSensor = idSensor,
            TipoRegra = TipoRegraAlerta.MediaMargemErro,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            TamanhoJanelaMedia = 50,
            MargemErro = 2,
            EmailNotificacao = "teste@exemplo.com",
            EstaAtivo = true
        };

        _mockRepositorioRegraAlerta
            .Setup(x => x.ObterAtivasPorIdSensorAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RegraAlerta> { regra1, regra2 });

        var estado1 = new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra1.Id,
            ContagemConsecutiva = 4,
            JsonMedicoesRecentes = "[]"
        };

        var medicoes = new List<decimal>();
        for (int i = 0; i < 49; i++)
        {
            medicoes.Add(0.5m);
        }

        var estado2 = new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra2.Id,
            ContagemConsecutiva = 0,
            JsonMedicoesRecentes = System.Text.Json.JsonSerializer.Serialize(medicoes)
        };

        _mockRepositorioEstadoAlerta
            .Setup(x => x.ObterPorRegraAlertaAsync(idSensor, regra1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado1);

        _mockRepositorioEstadoAlerta
            .Setup(x => x.ObterPorRegraAlertaAsync(idSensor, regra2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado2);

        var resultado = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 0.5m, DateTimeOffset.UtcNow);

        resultado.Should().HaveCount(2);

        _mockRepositorioHistoricoAlerta.Verify(
            x => x.AdicionarAsync(It.IsAny<HistoricoAlerta>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CasoLimite_ValoresExatosDoLimite()
    {
        var idSensor = 1;
        var regra = new RegraAlerta
        {
            Id = 1,
            IdSensor = idSensor,
            TipoRegra = TipoRegraAlerta.ConsecutivoForaIntervalo,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            ContagemConsecutiva = 5,
            EmailNotificacao = "teste@exemplo.com",
            EstaAtivo = true
        };

        _mockRepositorioRegraAlerta
            .Setup(x => x.ObterAtivasPorIdSensorAsync(idSensor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RegraAlerta> { regra });

        var estado = new EstadoAlerta
        {
            IdSensor = idSensor,
            IdRegraAlerta = regra.Id,
            ContagemConsecutiva = 0,
            JsonMedicoesRecentes = "[]"
        };

        _mockRepositorioEstadoAlerta
            .Setup(x => x.ObterPorRegraAlertaAsync(idSensor, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        var resultado1 = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 1m, DateTimeOffset.UtcNow);
        resultado1.Should().BeEmpty();

        var resultado50 = await _avaliadorAlerta.AvaliarAlertasAsync(idSensor, 50m, DateTimeOffset.UtcNow);
        resultado50.Should().BeEmpty();

        _mockRepositorioHistoricoAlerta.Verify(
            x => x.AdicionarAsync(It.IsAny<HistoricoAlerta>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
