using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SemearDadosAsync(ApplicationDbContext contexto)
    {
        if (contexto.Setores.Any())
        {
            return;
        }

        var setor1 = new Setor
        {
            Nome = "Chão de Produção",
            Descricao = "Área principal de produção",
            EstaAtivo = true
        };

        var setor2 = new Setor
        {
            Nome = "Armazém",
            Descricao = "Área de armazenamento",
            EstaAtivo = true
        };

        contexto.Setores.AddRange(setor1, setor2);
        await contexto.SaveChangesAsync();

        var equipamento1 = new Equipamento
        {
            Nome = "Compressor A",
            Descricao = "Compressor de ar principal",
            IdSetor = setor1.Id,
            EstaAtivo = true
        };

        var equipamento2 = new Equipamento
        {
            Nome = "Unidade de Resfriamento B",
            Descricao = "Sistema HVAC de resfriamento",
            IdSetor = setor1.Id,
            EstaAtivo = true
        };

        var equipamento3 = new Equipamento
        {
            Nome = "Monitor de Temperatura C",
            Descricao = "Controle de temperatura do armazém",
            IdSetor = setor2.Id,
            EstaAtivo = true
        };

        contexto.Equipamentos.AddRange(equipamento1, equipamento2, equipamento3);
        await contexto.SaveChangesAsync();

        var sensor1 = new Sensor
        {
            Codigo = "SENSOR001",
            Nome = "Sensor de Temperatura 1",
            Descricao = "Monitor de temperatura do compressor",
            EstaAtivo = true
        };

        var sensor2 = new Sensor
        {
            Codigo = "SENSOR002",
            Nome = "Sensor de Pressão 1",
            Descricao = "Monitor de pressão do compressor",
            EstaAtivo = true
        };

        var sensor3 = new Sensor
        {
            Codigo = "SENSOR003",
            Nome = "Sensor de Temperatura 2",
            Descricao = "Temperatura da unidade de resfriamento",
            EstaAtivo = true
        };

        contexto.Sensores.AddRange(sensor1, sensor2, sensor3);
        await contexto.SaveChangesAsync();

        var vinculo1 = new VinculoSensorEquipamento
        {
            IdSensor = sensor1.Id,
            IdEquipamento = equipamento1.Id,
            VinculadoPor = "system@example.com",
            EstaAtivo = true
        };

        var vinculo2 = new VinculoSensorEquipamento
        {
            IdSensor = sensor2.Id,
            IdEquipamento = equipamento1.Id,
            VinculadoPor = "system@example.com",
            EstaAtivo = true
        };

        var vinculo3 = new VinculoSensorEquipamento
        {
            IdSensor = sensor3.Id,
            IdEquipamento = equipamento2.Id,
            VinculadoPor = "system@example.com",
            EstaAtivo = true
        };

        contexto.VinculosSensorEquipamento.AddRange(vinculo1, vinculo2, vinculo3);
        await contexto.SaveChangesAsync();

        var regraAlerta1 = new RegraAlerta
        {
            IdSensor = sensor1.Id,
            TipoRegra = TipoRegraAlerta.ConsecutivoForaIntervalo,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            ContagemConsecutiva = 5,
            EmailNotificacao = "alerts@example.com",
            EstaAtivo = true
        };

        var regraAlerta2 = new RegraAlerta
        {
            IdSensor = sensor1.Id,
            TipoRegra = TipoRegraAlerta.MediaMargemErro,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            TamanhoJanelaMedia = 50,
            MargemErro = 2,
            EmailNotificacao = "alerts@example.com",
            EstaAtivo = true
        };

        var regraAlerta3 = new RegraAlerta
        {
            IdSensor = sensor2.Id,
            TipoRegra = TipoRegraAlerta.ConsecutivoForaIntervalo,
            LimiteMinimo = 1,
            LimiteMaximo = 50,
            ContagemConsecutiva = 5,
            EmailNotificacao = "alerts@example.com",
            EstaAtivo = true
        };

        contexto.RegrasAlerta.AddRange(regraAlerta1, regraAlerta2, regraAlerta3);
        await contexto.SaveChangesAsync();
    }
}
