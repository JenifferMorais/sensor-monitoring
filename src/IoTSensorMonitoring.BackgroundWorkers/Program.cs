using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.BackgroundWorkers.Workers;
using IoTSensorMonitoring.Domain.Interfaces;
using IoTSensorMonitoring.Infrastructure.Persistence;
using IoTSensorMonitoring.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IRepositorioHistoricoAlerta, RepositorioHistoricoAlerta>();
builder.Services.AddScoped<IRepositorioRegraAlerta, RepositorioRegraAlerta>();
builder.Services.AddScoped<IRepositorioEstadoAlerta, RepositorioEstadoAlerta>();
builder.Services.AddScoped<IRepositorioSensor, RepositorioSensor>();

builder.Services.AddScoped<ServicoAvaliacaoAlerta>();

builder.Services.AddHostedService<TrabalhadorNotificacaoAlerta>();
builder.Services.AddHostedService<ConsumidorMedicoes>();

var host = builder.Build();
host.Run();
