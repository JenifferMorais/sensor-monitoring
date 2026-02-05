using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Application.Validators;
using IoTSensorMonitoring.BackgroundWorkers.Workers;
using IoTSensorMonitoring.Domain.Interfaces;
using IoTSensorMonitoring.Infrastructure.Messaging;
using IoTSensorMonitoring.Infrastructure.Persistence;
using IoTSensorMonitoring.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.EnableAnnotations();
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IRepositorioMedicao, RepositorioMedicao>();
builder.Services.AddScoped<IRepositorioSensor, RepositorioSensor>();
builder.Services.AddScoped<IRepositorioEquipamento, RepositorioEquipamento>();
builder.Services.AddScoped<IRepositorioVinculoSensorEquipamento, RepositorioVinculoSensorEquipamento>();
builder.Services.AddScoped<IRepositorioRegraAlerta, RepositorioRegraAlerta>();
builder.Services.AddScoped<IRepositorioEstadoAlerta, RepositorioEstadoAlerta>();
builder.Services.AddScoped<IRepositorioHistoricoAlerta, RepositorioHistoricoAlerta>();

builder.Services.AddScoped<ServicoMedicao>();
builder.Services.AddScoped<ServicoVinculoSensorEquipamento>();
builder.Services.AddScoped<ServicoConsultaSensor>();
builder.Services.AddScoped<ServicoConsultaEquipamento>();
builder.Services.AddScoped<ServicoConsultaMedicao>();
builder.Services.AddScoped<ServicoAvaliacaoAlerta>();

builder.Services.AddScoped<IValidator<RequisicaoMedicaoUnica>, ValidadorRequisicaoMedicaoUnica>();
builder.Services.AddScoped<IValidator<RequisicaoLoteMedicoes>, ValidadorRequisicaoLoteMedicoes>();

builder.Services.AddSingleton<IPublicadorMensagens, PublicadorRabbitMq>();
builder.Services.AddHostedService<ConsumidorMedicoes>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Monitoramento");
        options.DocumentTitle = "Documentação - API IoT Sensor Monitoring";
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
