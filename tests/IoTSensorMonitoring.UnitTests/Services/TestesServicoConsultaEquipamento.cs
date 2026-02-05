using FluentAssertions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace IoTSensorMonitoring.UnitTests.Services;

public class TestesServicoConsultaEquipamento
{
    private readonly Mock<IRepositorioEquipamento> _mockRepositorioEquipamento;
    private readonly Mock<ILogger<ServicoConsultaEquipamento>> _mockLogger;
    private readonly ServicoConsultaEquipamento _servico;

    public TestesServicoConsultaEquipamento()
    {
        _mockRepositorioEquipamento = new Mock<IRepositorioEquipamento>();
        _mockLogger = new Mock<ILogger<ServicoConsultaEquipamento>>();

        _servico = new ServicoConsultaEquipamento(
            _mockRepositorioEquipamento.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CriarAsync_DeveCriarEquipamento_ComDadosValidos()
    {
        var requisicao = new RequisicaoCriarEquipamento
        {
            Nome = "Equipamento Teste",
            Descricao = "Descrição teste",
            IdSetor = 1,
            EstaAtivo = true
        };

        var equipamentoCriado = new Equipamento
        {
            Id = 1,
            Nome = requisicao.Nome,
            Descricao = requisicao.Descricao,
            IdSetor = requisicao.IdSetor,
            EstaAtivo = requisicao.EstaAtivo,
            Setor = new Setor { Id = 1, Nome = "Setor Teste" },
            VinculosSensorEquipamento = new List<VinculoSensorEquipamento>()
        };

        _mockRepositorioEquipamento
            .Setup(x => x.AdicionarAsync(It.IsAny<Equipamento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Equipamento, CancellationToken>((eq, _) => eq.Id = 1);

        _mockRepositorioEquipamento
            .Setup(x => x.ObterPorIdComSensoresAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipamentoCriado);

        var resultado = await _servico.CriarAsync(requisicao);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados.Should().NotBeNull();
        resultado.Dados!.Nome.Should().Be(requisicao.Nome);
        resultado.Dados.Descricao.Should().Be(requisicao.Descricao);
        resultado.Dados.EstaAtivo.Should().Be(requisicao.EstaAtivo);

        _mockRepositorioEquipamento.Verify(
            x => x.AdicionarAsync(It.IsAny<Equipamento>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarEquipamento_QuandoExistir()
    {
        var idEquipamento = 1;
        var equipamento = new Equipamento
        {
            Id = idEquipamento,
            Nome = "Equipamento Teste",
            Descricao = "Descrição",
            IdSetor = 1,
            EstaAtivo = true,
            Setor = new Setor { Id = 1, Nome = "Setor Teste" },
            VinculosSensorEquipamento = new List<VinculoSensorEquipamento>()
        };

        _mockRepositorioEquipamento
            .Setup(x => x.ObterPorIdComSensoresAsync(idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipamento);

        var resultado = await _servico.ObterPorIdAsync(idEquipamento);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados.Should().NotBeNull();
        resultado.Dados!.Id.Should().Be(idEquipamento);
        resultado.Dados.Nome.Should().Be("Equipamento Teste");
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarNaoEncontrado_QuandoNaoExistir()
    {
        var idEquipamento = 999;

        _mockRepositorioEquipamento
            .Setup(x => x.ObterPorIdComSensoresAsync(idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Equipamento?)null);

        var resultado = await _servico.ObterPorIdAsync(idEquipamento);

        resultado.Sucesso.Should().BeFalse();
        resultado.CodigoHttp.Should().Be(404);
        resultado.Erro.Should().Contain("não encontrado");
    }

    [Fact]
    public async Task AtualizarAsync_DeveAtualizarEquipamento_QuandoExistir()
    {
        var idEquipamento = 1;
        var equipamentoExistente = new Equipamento
        {
            Id = idEquipamento,
            Nome = "Nome Antigo",
            Descricao = "Descrição Antiga",
            IdSetor = 1,
            EstaAtivo = true
        };

        var requisicao = new RequisicaoAtualizarEquipamento
        {
            Nome = "Nome Novo",
            Descricao = "Descrição Nova",
            IdSetor = 2,
            EstaAtivo = false
        };

        _mockRepositorioEquipamento
            .Setup(x => x.ObterPorIdAsync(idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipamentoExistente);

        _mockRepositorioEquipamento
            .Setup(x => x.AtualizarAsync(It.IsAny<Equipamento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var resultado = await _servico.AtualizarAsync(idEquipamento, requisicao);

        resultado.Sucesso.Should().BeTrue();
        equipamentoExistente.Nome.Should().Be("Nome Novo");
        equipamentoExistente.Descricao.Should().Be("Descrição Nova");
        equipamentoExistente.IdSetor.Should().Be(2);
        equipamentoExistente.EstaAtivo.Should().BeFalse();

        _mockRepositorioEquipamento.Verify(
            x => x.AtualizarAsync(It.IsAny<Equipamento>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeletarAsync_DeveDeletarEquipamento_QuandoExistir()
    {
        var idEquipamento = 1;

        _mockRepositorioEquipamento
            .Setup(x => x.DeletarAsync(idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultado = await _servico.DeletarAsync(idEquipamento);

        resultado.Sucesso.Should().BeTrue();
        resultado.Mensagem.Should().Contain("excluído");

        _mockRepositorioEquipamento.Verify(
            x => x.DeletarAsync(idEquipamento, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeletarAsync_DeveRetornarNaoEncontrado_QuandoNaoExistir()
    {
        var idEquipamento = 999;

        _mockRepositorioEquipamento
            .Setup(x => x.DeletarAsync(idEquipamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await _servico.DeletarAsync(idEquipamento);

        resultado.Sucesso.Should().BeFalse();
        resultado.CodigoHttp.Should().Be(404);
        resultado.Erro.Should().Contain("não encontrado");
    }

    [Fact]
    public async Task ListarPaginadoAsync_DeveRetornarListaPaginada()
    {
        var equipamentos = new List<Equipamento>
        {
            new Equipamento
            {
                Id = 1,
                Nome = "Equipamento 1",
                IdSetor = 1,
                Setor = new Setor { Id = 1, Nome = "Setor 1" },
                VinculosSensorEquipamento = new List<VinculoSensorEquipamento>()
            },
            new Equipamento
            {
                Id = 2,
                Nome = "Equipamento 2",
                IdSetor = 1,
                Setor = new Setor { Id = 1, Nome = "Setor 1" },
                VinculosSensorEquipamento = new List<VinculoSensorEquipamento>()
            }
        };

        _mockRepositorioEquipamento
            .Setup(x => x.ListarPaginadoAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((equipamentos, 2));

        var resultado = await _servico.ListarPaginadoAsync(1, 10);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados.Should().NotBeNull();
        resultado.Dados!.Itens.Should().HaveCount(2);
        resultado.Dados.TotalItens.Should().Be(2);
        resultado.Dados.PaginaAtual.Should().Be(1);
        resultado.Dados.TamanhoPagina.Should().Be(10);
    }
}
