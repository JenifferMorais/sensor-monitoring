# Respostas - Teste DEV Pleno

## PARTE 01

### a) Qual/Quais API(s) você criaria para receber esses dados? Justifique sua resposta.

Eu criaria uma única API com dois endpoints:

- `POST /api/v1/medicoes` - pra receber medição individual
- `POST /api/v1/medicoes/lote` - pra receber várias medições de uma vez

Pensei assim: o firmware às vezes consegue enviar na hora, outras vezes acumula dados quando o WiFi tá ruim. Faz mais sentido ter os dois endpoints na mesma API porque a lógica de validação e salvamento no banco é basicamente a mesma.

Separar em duas APIs diferentes ia gerar duplicação desnecessária de código, e o firmware precisaria de duas URLs diferentes pra gerenciar. Mantendo tudo junto fica mais simples de manter e evoluir

### b) Como você definiria o objeto de request dessa API no Asp.Net Core?

```csharp
// DTOs de request
public class MedicaoRequest
{
    public int IdSensor { get; set; }
    public string CodigoSensor { get; set; } = string.Empty;
    public DateTimeOffset DataHoraMedicao { get; set; }
    public decimal ValorMedicao { get; set; }
}

public class LoteMedicoesRequest
{
    public List<MedicaoRequest> Medicoes { get; set; } = new();
}

// Controller
[ApiController]
[Route("api/v1/[controller]")]
public class MedicoesController : ControllerBase
{
    private readonly IMedicaoService _service;
    private readonly IValidator<MedicaoRequest> _validadorMedicao;
    private readonly IValidator<LoteMedicoesRequest> _validadorLote;

    public MedicoesController(
        IMedicaoService service,
        IValidator<MedicaoRequest> validadorMedicao,
        IValidator<LoteMedicoesRequest> validadorLote)
    {
        _service = service;
        _validadorMedicao = validadorMedicao;
        _validadorLote = validadorLote;
    }

    [HttpPost]
    public async Task<IActionResult> ReceberMedicao(
        [FromBody] MedicaoRequest request,
        CancellationToken ct)
    {
        var validacao = await _validadorMedicao.ValidateAsync(request, ct);
        if (!validacao.IsValid)
            return BadRequest(new { erros = validacao.Errors });

        var resultado = await _service.ProcessarMedicaoAsync(request, ct);
        return Ok(new { sucesso = resultado.Sucesso });
    }

    [HttpPost("lote")]
    public async Task<IActionResult> ReceberLoteMedicoes(
        [FromBody] LoteMedicoesRequest request,
        CancellationToken ct)
    {
        var validacao = await _validadorLote.ValidateAsync(request, ct);
        if (!validacao.IsValid)
            return BadRequest(new { erros = validacao.Errors });

        var resultado = await _service.ProcessarLoteAsync(request.Medicoes, ct);
        return Ok(new
        {
            sucesso = resultado.Sucesso,
            processadas = request.Medicoes.Count
        });
    }
}
```

### c) Qual a melhor alternativa de banco de dados para esse cenário?

Eu iria de **PostgreSQL com TimescaleDB**.

TimescaleDB é uma extensão do Postgres feita especificamente pra dados de séries temporais, que é exatamente o nosso caso aqui. Os sensores vão gerar dados continuamente com timestamp.

Por que escolhi essa stack:

**Performance:** O TimescaleDB cria hypertables que fazem particionamento automático por tempo. Isso deixa as queries muito mais rápidas quando você busca "últimas N medições" ou "medições do último mês".

**Compressão:** Dados antigos são comprimidos automaticamente (chega a 90% de economia de espaço). Como vamos acumular muita medição ao longo do tempo, isso vai economizar bastante storage.

**Relacionamentos:** Diferente de bancos NoSQL ou InfluxDB, aqui consigo manter foreign keys normalmente pra vincular Sensor → Equipamento → Setor. Melhor dos dois mundos.

**Agregações:** Dá pra configurar continuous aggregates que já pré-calculam médias por hora/dia. Útil pra dashboards e relatórios.

**Custo:** É open-source, então não tem licença cara igual SQL Server.

Cheguei a considerar InfluxDB (que é especializado em time-series), mas ia complicar na hora de fazer os vínculos relacionais. MongoDB seria flexível mas não tem as otimizações específicas pra esse tipo de dado.

---

## PARTE 02

### a) API para vincular Sensor a Equipamento

```csharp
public class VincularSensorRequest
{
    public string VinculadoPor { get; set; } = string.Empty;
}

[ApiController]
[Route("api/v1/sensores")]
public class SensoresController : ControllerBase
{
    private readonly IVinculoService _vinculoService;

    [HttpPost("{idSensor}/equipamento/{idEquipamento}")]
    public async Task<IActionResult> VincularSensor(
        int idSensor,
        int idEquipamento,
        [FromBody] VincularSensorRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.VinculadoPor))
            return BadRequest(new { erro = "Campo 'VinculadoPor' é obrigatório" });

        var resultado = await _vinculoService.VincularAsync(
            idSensor,
            idEquipamento,
            request.VinculadoPor,
            ct);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Mensagem });

        return Ok(new { mensagem = resultado.Mensagem });
    }
}

// Service - lógica de negócio
public class VinculoService : IVinculoService
{
    private readonly ISensorRepository _sensorRepo;
    private readonly IEquipamentoRepository _equipRepo;
    private readonly IVinculoRepository _vinculoRepo;

    public async Task<(bool Sucesso, string Mensagem)> VincularAsync(
        int idSensor,
        int idEquipamento,
        string vinculadoPor,
        CancellationToken ct = default)
    {
        // Validações básicas
        var sensor = await _sensorRepo.GetByIdAsync(idSensor, ct);
        if (sensor == null)
            return (false, "Sensor não encontrado");

        var equipamento = await _equipRepo.GetByIdAsync(idEquipamento, ct);
        if (equipamento == null)
            return (false, "Equipamento não encontrado");

        // Verifica se já existe vínculo ativo
        var vinculoAtivo = await _vinculoRepo.GetVinculoAtivoAsync(idSensor, idEquipamento, ct);
        if (vinculoAtivo != null)
            return (false, "Sensor já vinculado a este equipamento");

        // Cria o vínculo
        var novoVinculo = new VinculoSensorEquipamento
        {
            IdSensor = idSensor,
            IdEquipamento = idEquipamento,
            VinculadoPor = vinculadoPor,
            VinculadoEm = DateTimeOffset.UtcNow,
            Ativo = true
        };

        await _vinculoRepo.AddAsync(novoVinculo, ct);
        return (true, "Sensor vinculado com sucesso");
    }
}
```

### b) API para buscar últimas 10 medições por sensor do equipamento

```csharp
[ApiController]
[Route("api/v1/equipamentos")]
public class EquipamentosController : ControllerBase
{
    private readonly IMedicaoService _medicaoService;

    [HttpGet("{idEquipamento}/medicoes/recentes")]
    public async Task<IActionResult> GetUltimasMedicoes(
        int idEquipamento,
        [FromQuery] int limite = 10,
        CancellationToken ct = default)
    {
        var medicoes = await _medicaoService.GetUltimasMedicoesPorSensorAsync(
            idEquipamento,
            limite,
            ct);

        if (medicoes == null || !medicoes.Any())
            return NotFound(new { erro = "Nenhuma medição encontrada para este equipamento" });

        return Ok(medicoes);
    }
}

// Repository - query otimizada
public class MedicaoRepository : IMedicaoRepository
{
    private readonly AppDbContext _context;

    public async Task<List<MedicaoDto>> GetUltimasPorEquipamentoAsync(
        int idEquipamento,
        int limitePorSensor,
        CancellationToken ct = default)
    {
        // Usando window function pra pegar as N últimas de cada sensor
        // Muito mais performático que fazer múltiplas queries
        var sql = @"
            WITH MedicoesRanqueadas AS (
                SELECT
                    m.Id,
                    m.IdSensor,
                    m.DataHoraMedicao,
                    m.ValorMedicao,
                    ROW_NUMBER() OVER (
                        PARTITION BY m.IdSensor
                        ORDER BY m.DataHoraMedicao DESC
                    ) as RowNum
                FROM Medicoes m
                INNER JOIN VinculosSensorEquipamento v
                    ON m.IdSensor = v.IdSensor
                WHERE v.IdEquipamento = @equipamentoId
                  AND v.Ativo = true
            )
            SELECT Id, IdSensor, DataHoraMedicao, ValorMedicao
            FROM MedicoesRanqueadas
            WHERE RowNum <= @limite
            ORDER BY IdSensor, DataHoraMedicao DESC";

        return await _context.Database
            .SqlQueryRaw<MedicaoDto>(sql,
                new SqlParameter("@equipamentoId", idEquipamento),
                new SqlParameter("@limite", limitePorSensor))
            .ToListAsync(ct);
    }
}
```

---

## PARTE 03

### a) Proposta de solução para sistema de alertas

Minha abordagem seria processar os alertas de forma híbrida:

**Fluxo:**
```
Medição → Valida → Salva no DB → Checa regras de alerta (sync)
                                         ↓
                                  Alerta disparado?
                                         ↓
                           Salva no histórico + adiciona na fila
                                         ↓
                                 Worker em background
                                         ↓
                                   Envia e-mail
```

**Como funcionaria:**

Quando uma medição chega, avalio as regras de alerta de forma síncrona (é rápido, só matemática). Se disparar alguma regra, salvo no histórico e marco pra enviar email.

O envio de email roda em background num `IHostedService` que fica pooling a tabela de alertas pendentes a cada 30-60 segundos. Assim não bloqueia o recebimento das medições.

**Estrutura no banco:**

- `RegraAlerta`: configuração das regras (limites, emails, etc)
- `EstadoAlerta`: guarda o estado atual de cada sensor (contador de consecutivas, janela das últimas 50 medições)
- `HistoricoAlerta`: log de todos os alertas disparados (audit trail)

Armazeno a janela móvel das 50 medições em JSONB, funciona bem pra esse volume de dados.

**Sobre usar fila (RabbitMQ/Redis):**

Pra esse caso específico, acho que seria over-engineering. O volume não justifica adicionar mais uma peça na infra. Se futuramente começar a ter milhares de alertas por minuto, aí sim vale migrar pra uma fila dedicada.

### b) Algoritmo de detecção de alertas

```csharp
public class DetectorAlerta
{
    private readonly IEstadoAlertaRepository _estadoRepo;
    private readonly IHistoricoAlertaRepository _historicoRepo;

    // Regra 1: Mais de 5 medições consecutivas fora do range [1, 50]
    private async Task<Alerta?> CheckConsecutivasForaRange(
        int idSensor,
        RegraAlerta regra,
        decimal valorMedicao,
        DateTimeOffset timestamp,
        CancellationToken ct)
    {
        var estado = await _estadoRepo.GetOrCreateAsync(idSensor, regra.Id, ct);

        bool foraDoLimite = valorMedicao < regra.Min || valorMedicao > regra.Max;

        if (foraDoLimite)
        {
            estado.Contador++;

            // Chegou em 5 consecutivas? Dispara alerta
            if (estado.Contador >= 5)
            {
                var alerta = new Alerta
                {
                    IdSensor = idSensor,
                    Tipo = "CONSECUTIVAS_FORA_RANGE",
                    Mensagem = $"{estado.Contador} medições consecutivas fora do range [{regra.Min}, {regra.Max}]",
                    ValorAtual = valorMedicao,
                    EmailDestino = regra.Email,
                    DisparadoEm = timestamp
                };

                // Reseta contador e salva histórico
                estado.Contador = 0;
                await _estadoRepo.UpdateAsync(estado, ct);
                await _historicoRepo.AddAsync(alerta, ct);

                return alerta;
            }
        }
        else
        {
            // Voltou pro normal? Zera contador
            estado.Contador = 0;
        }

        await _estadoRepo.UpdateAsync(estado, ct);
        return null;
    }

    // Regra 2: Média das últimas 50 na margem de erro (±2 dos limites)
    private async Task<Alerta?> CheckMediaMargemErro(
        int idSensor,
        RegraAlerta regra,
        decimal valorMedicao,
        DateTimeOffset timestamp,
        CancellationToken ct)
    {
        var estado = await _estadoRepo.GetOrCreateAsync(idSensor, regra.Id, ct);

        // Deserializa janela de medições (guardada em JSONB)
        var janela = new List<decimal>();
        if (!string.IsNullOrEmpty(estado.JanelaJson))
        {
            try
            {
                janela = JsonSerializer.Deserialize<List<decimal>>(estado.JanelaJson) ?? new();
            }
            catch
            {
                janela = new();
            }
        }

        // Adiciona nova medição e mantém só as últimas 50
        janela.Add(valorMedicao);
        if (janela.Count > 50)
            janela.RemoveAt(0);

        estado.JanelaJson = JsonSerializer.Serialize(janela);
        await _estadoRepo.UpdateAsync(estado, ct);

        // Só calcula se já tiver as 50 medições
        if (janela.Count < 50)
            return null;

        decimal media = janela.Average();

        // Verifica se tá na margem de erro
        // Margem inferior: [1-2, 1+2] = [-1, 3]
        // Margem superior: [50-2, 50+2] = [48, 52]
        bool naMargemInf = media >= (regra.Min - 2) && media <= (regra.Min + 2);
        bool naMargemSup = media >= (regra.Max - 2) && media <= (regra.Max + 2);

        if (naMargemInf || naMargemSup)
        {
            var zona = naMargemInf ? "inferior" : "superior";
            var alerta = new Alerta
            {
                IdSensor = idSensor,
                Tipo = "MEDIA_MARGEM_ERRO",
                Mensagem = $"Média das últimas 50 medições na margem {zona}: {media:F2}",
                ValorAtual = media,
                EmailDestino = regra.Email,
                DisparadoEm = timestamp
            };

            await _historicoRepo.AddAsync(alerta, ct);
            return alerta;
        }

        return null;
    }
}
```

### c) Testes Unitários

```csharp
public class DetectorAlertaTests
{
    private readonly Mock<IEstadoAlertaRepository> _mockEstadoRepo;
    private readonly Mock<IHistoricoAlertaRepository> _mockHistoricoRepo;
    private readonly DetectorAlerta _detector;

    public DetectorAlertaTests()
    {
        _mockEstadoRepo = new Mock<IEstadoAlertaRepository>();
        _mockHistoricoRepo = new Mock<IHistoricoAlertaRepository>();
        _detector = new DetectorAlerta(_mockEstadoRepo.Object, _mockHistoricoRepo.Object);
    }

    [Fact]
    public async Task DeveDispararAlerta_QuandoTiver5ConsecutivasForaDoRange()
    {
        // Arrange
        var regra = new RegraAlerta { Min = 1, Max = 50, Email = "test@test.com" };
        var estado = new EstadoAlerta { Contador = 0 };

        _mockEstadoRepo
            .Setup(x => x.GetOrCreateAsync(1, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        // Act - primeiras 4 não disparam
        for (int i = 0; i < 4; i++)
        {
            var result = await _detector.CheckConsecutivasForaRange(1, regra, 0.5m, DateTimeOffset.Now, default);
            Assert.Null(result);
            estado.Contador = i + 1;
        }

        // Act - 5ª dispara
        var alerta = await _detector.CheckConsecutivasForaRange(1, regra, 0.5m, DateTimeOffset.Now, default);

        // Assert
        Assert.NotNull(alerta);
        Assert.Equal("CONSECUTIVAS_FORA_RANGE", alerta.Tipo);
        _mockHistoricoRepo.Verify(x => x.AddAsync(It.IsAny<Alerta>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeveResetarContador_QuandoValorVoltarAoNormal()
    {
        // Arrange
        var regra = new RegraAlerta { Min = 1, Max = 50 };
        var estado = new EstadoAlerta { Contador = 3 };

        _mockEstadoRepo
            .Setup(x => x.GetOrCreateAsync(1, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        // Act - valor dentro do range
        var alerta = await _detector.CheckConsecutivasForaRange(1, regra, 25m, DateTimeOffset.Now, default);

        // Assert
        Assert.Null(alerta);
        Assert.Equal(0, estado.Contador); // resetou
    }

    [Fact]
    public async Task DeveDispararAlerta_QuandoMediaEstiverNaMargemInferior()
    {
        // Arrange - 49 medições de 0.5
        var regra = new RegraAlerta { Min = 1, Max = 50, Email = "test@test.com" };
        var medicoes = Enumerable.Repeat(0.5m, 49).ToList();
        var estado = new EstadoAlerta
        {
            JanelaJson = JsonSerializer.Serialize(medicoes)
        };

        _mockEstadoRepo
            .Setup(x => x.GetOrCreateAsync(1, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        // Act - 50ª medição completa a janela
        var alerta = await _detector.CheckMediaMargemErro(1, regra, 0.5m, DateTimeOffset.Now, default);

        // Assert - média = 0.5, está em [-1, 3] (margem inferior)
        Assert.NotNull(alerta);
        Assert.Equal("MEDIA_MARGEM_ERRO", alerta.Tipo);
        Assert.Contains("inferior", alerta.Mensagem);
    }

    [Fact]
    public async Task NaoDeveDispararAlerta_QuandoMediaEstiverNaZonaSegura()
    {
        // Arrange - 50 medições de 25 (bem no meio do range)
        var medicoes = Enumerable.Repeat(25m, 50).ToList();
        var estado = new EstadoAlerta { JanelaJson = JsonSerializer.Serialize(medicoes) };
        var regra = new RegraAlerta { Min = 1, Max = 50 };

        _mockEstadoRepo
            .Setup(x => x.GetOrCreateAsync(1, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        // Act
        var alerta = await _detector.CheckMediaMargemErro(1, regra, 25m, DateTimeOffset.Now, default);

        // Assert
        Assert.Null(alerta);
    }

    [Fact]
    public async Task NaoDeveAvaliarMedia_QuandoTiverMenosDe50Medicoes()
    {
        // Arrange - só 30 medições
        var medicoes = Enumerable.Repeat(0.5m, 30).ToList();
        var estado = new EstadoAlerta { JanelaJson = JsonSerializer.Serialize(medicoes) };
        var regra = new RegraAlerta { Min = 1, Max = 50 };

        _mockEstadoRepo
            .Setup(x => x.GetOrCreateAsync(1, regra.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estado);

        // Act
        var alerta = await _detector.CheckMediaMargemErro(1, regra, 0.5m, DateTimeOffset.Now, default);

        // Assert
        Assert.Null(alerta);
        _mockHistoricoRepo.Verify(x => x.AddAsync(It.IsAny<Alerta>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

**Outros cenários que testaria:**
- Valor exatamente no limite (1.0 e 50.0)
- Média exatamente na borda da margem (48.0, 52.0, -1.0, 3.0)
- Alternância entre dentro/fora (não deve acumular contador)
- Múltiplos sensores disparando ao mesmo tempo

---

## PARTE 04

### Soluções para problema de escalabilidade

Quando o volume de dados começa a gerar atraso, precisa atacar em várias frentes:

#### 1. Desacoplar recebimento do processamento

A API hoje provavelmente tá fazendo tudo de forma síncrona: recebe → valida → salva → retorna. Isso não escala.

Mudaria pra:
```
API → Valida básico → Joga na fila (RabbitMQ) → Return 202 Accepted
                              ↓
                    Workers em background
                              ↓
                    Processa em lote (1000-5000/vez)
                              ↓
                    Bulk insert no Postgres
```

Dessa forma a API responde em milissegundos e o trabalho pesado fica pros workers. Posso escalar workers independente da API.

#### 2. Bulk Insert com COPY

INSERT linha por linha é muito lento. O comando COPY do Postgres é otimizado pra inserção em massa:

```csharp
using var writer = conn.BeginBinaryImport(
    "COPY Medicoes (IdSensor, DataHoraMedicao, ValorMedicao, RecebidoEm) FROM STDIN (FORMAT BINARY)");

foreach (var medicao in lote)
{
    writer.StartRow();
    writer.Write(medicao.IdSensor);
    writer.Write(medicao.DataHoraMedicao);
    writer.Write(medicao.ValorMedicao);
    writer.Write(DateTimeOffset.UtcNow);
}

await writer.CompleteAsync();
```

Já vi isso dar ganho de 50-100x dependendo do tamanho do lote.

#### 3. Escalar horizontalmente

- **API**: Colocar 3+ instâncias atrás de um load balancer (nginx ou ALB)
- **Workers**: 5-10 workers consumindo da fila em paralelo
- **Banco**: Read replicas pras queries (leituras). Escritas vão pro primary.

#### 4. Otimizações do TimescaleDB

Como escolhi TimescaleDB antes, agora é usar os recursos dele:

```sql
-- Ativa compressão automática (economiza 70-90% de espaço)
SELECT add_compression_policy('Medicoes', INTERVAL '7 days');

-- Remove dados antigos automaticamente
SELECT add_retention_policy('Medicoes', INTERVAL '1 year');

-- Pre-calcula agregações pra dashboards
CREATE MATERIALIZED VIEW medicoes_por_hora
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', DataHoraMedicao) AS hora,
    IdSensor,
    AVG(ValorMedicao) as media,
    COUNT(*) as total
FROM Medicoes
GROUP BY hora, IdSensor;
```

Isso deixa consultas de dashboard absurdamente mais rápidas.

#### 5. Cache com Redis

Cachear o que for acessado frequentemente:

- Últimas medições de cada sensor (TTL de 5 min)
- Estado dos alertas
- Metadados dos sensores/equipamentos

Isso pode reduzir 70-80% da carga no banco pra queries repetitivas.

#### 6. Connection Pool bem configurado

```json
{
  "ConnectionStrings": {
    "Default": "Host=db;Database=iot;Pooling=true;MinPoolSize=20;MaxPoolSize=200;CommandTimeout=30"
  }
}
```

Pool pequeno = workers esperando conexão disponível
Pool grande demais = overhead no banco

#### 7. Auto-scaling (se rodar em Kubernetes)

Configurar HPA pra escalar workers baseado no tamanho da fila:

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: medicao-workers
spec:
  scaleTargetRef:
    name: medicao-worker
  minReplicas: 5
  maxReplicas: 30
  metrics:
  - type: External
    external:
      metric:
        name: rabbitmq_queue_depth
      target:
        averageValue: "1000"
```

Quando a fila encher, sobe mais workers automaticamente.

#### Resultado esperado

Com essas mudanças, esperaria conseguir:
- **Ingestão**: 20k-50k medições/segundo
- **Latência da API**: < 50ms (P95)
- **Processamento de alertas**: < 2 segundos
- **Queries**: < 100ms (P95)
