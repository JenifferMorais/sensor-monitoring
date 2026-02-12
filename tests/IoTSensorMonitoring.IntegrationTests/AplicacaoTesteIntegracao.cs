using IoTSensorMonitoring.BackgroundWorkers.Workers;
using IoTSensorMonitoring.Domain.Interfaces;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace IoTSensorMonitoring.IntegrationTests;

public class AplicacaoTesteIntegracao : WebApplicationFactory<Program>
{
    private readonly string _nomeBancoDados = $"TesteIntegracao_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(IPublicadorMensagens));

            var consumidor = services.Where(d => d.ImplementationType == typeof(ConsumidorMedicoes)).ToList();
            foreach (var service in consumidor)
                services.Remove(service);

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_nomeBancoDados);
            });

            services.AddSingleton<IPublicadorMensagens>(sp =>
            {
                var mock = new Mock<IPublicadorMensagens>();
                return mock.Object;
            });

            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            SeedDatabase(db);
        });
    }

    private static void SeedDatabase(ApplicationDbContext db)
    {
        var setor = new Domain.Entities.Setor
        {
            Nome = "Setor Teste",
            Descricao = "Setor para testes de integração"
        };
        db.Setores.Add(setor);
        db.SaveChanges();

        var equipamento = new Domain.Entities.Equipamento
        {
            Nome = "Equipamento Teste 1",
            Descricao = "Equipamento para testes",
            IdSetor = setor.Id,
            EstaAtivo = true
        };
        db.Equipamentos.Add(equipamento);
        db.SaveChanges();

        var sensor = new Domain.Entities.Sensor
        {
            Codigo = "SENSOR001",
            Nome = "Sensor de Teste 1",
            EstaAtivo = true
        };
        db.Sensores.Add(sensor);
        db.SaveChanges();
    }
}
