# Sistema de Monitoramento de Sensores IoT

Sistema de monitoramento e análise de dados de sensores IoT com suporte a ingestão em lote, alertas automáticos e visualização de medições por equipamento.

## Stack Tecnológico

- .NET 10
- PostgreSQL + TimescaleDB
- Entity Framework Core
- FluentValidation
- xUnit + Moq

## Estrutura do Projeto

```
src/
├── IoTSensorMonitoring.Domain/          # Entidades e interfaces
├── IoTSensorMonitoring.Application/     # Lógica de negócio e DTOs
├── IoTSensorMonitoring.Infrastructure/  # Repositórios e persistência
├── IoTSensorMonitoring.API/             # Controllers REST
└── IoTSensorMonitoring.BackgroundWorkers/ # Processamento de alertas

tests/
├── IoTSensorMonitoring.UnitTests/
└── IoTSensorMonitoring.IntegrationTests/
```

## Principais Funcionalidades

### Ingestão de Dados
- POST `/api/v1/measurements` - medição individual
- POST `/api/v1/measurements/batch` - lote de medições

### Vinculação Sensor-Equipamento
- POST `/api/v1/sensors/{id}/equipment/{id}/link`
- GET `/api/v1/equipment/{id}/measurements/latest` - últimas 10 medições por sensor

### Sistema de Alertas

**Tipos de regra:**
1. ConsecutiveOutOfRange - dispara após N medições consecutivas fora do intervalo
2. AverageMarginOfError - dispara quando média móvel entra em zona de margem

Os alertas são processados em background e geram notificações por email.

## Configuração

### Banco de Dados

Configurar connection string em `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=iotsensormonitoring;Username=postgres;Password=postgres"
  }
}
```

Executar migrations:

```bash
cd src/IoTSensorMonitoring.API
dotnet ef database update
```

### Docker Compose

```bash
docker-compose up -d
```

Sobe PostgreSQL, Redis e RabbitMQ para desenvolvimento local.

## Executar

```bash
# API
cd src/IoTSensorMonitoring.API
dotnet run

# Background Workers
cd src/IoTSensorMonitoring.BackgroundWorkers
dotnet run

# Testes
dotnet test
```

## Validações

- ID do sensor > 0
- Código obrigatório (max 50 caracteres)
- Data da medição não pode ser futura nem anterior a 1 ano
- Valor da medição entre -273.15 e 1000
- Lotes limitados a 10.000 medições

## Observações

O sistema usa TimescaleDB para otimização de séries temporais. As medições são particionadas automaticamente e queries de range são extremamente rápidas.

A avaliação de alertas ocorre de forma síncrona durante a ingestão, mas o envio de emails é assíncrono via worker em background.
