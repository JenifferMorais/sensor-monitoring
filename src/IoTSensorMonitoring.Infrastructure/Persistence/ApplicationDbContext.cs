using IoTSensorMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Sensor> Sensores => Set<Sensor>();
    public DbSet<Medicao> Medicoes => Set<Medicao>();
    public DbSet<Setor> Setores => Set<Setor>();
    public DbSet<Equipamento> Equipamentos => Set<Equipamento>();
    public DbSet<VinculoSensorEquipamento> VinculosSensorEquipamento => Set<VinculoSensorEquipamento>();
    public DbSet<RegraAlerta> RegrasAlerta => Set<RegraAlerta>();
    public DbSet<EstadoAlerta> EstadosAlerta => Set<EstadoAlerta>();
    public DbSet<HistoricoAlerta> HistoricoAlertas => Set<HistoricoAlerta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Sensor - apenas índices
        modelBuilder.Entity<Sensor>()
            .HasIndex(e => e.Codigo).IsUnique();

        // Medicao - precisão e índices
        modelBuilder.Entity<Medicao>(entity =>
        {
            entity.Property(e => e.ValorMedicao).HasPrecision(18, 4);
            entity.HasIndex(e => new { e.IdSensor, e.DataHoraMedicao });
            entity.HasIndex(e => e.IdLote);
        });

        // Equipamento - comportamento de exclusão
        modelBuilder.Entity<Equipamento>()
            .HasOne(e => e.Setor)
            .WithMany(s => s.Equipamentos)
            .OnDelete(DeleteBehavior.Restrict);

        // VinculoSensorEquipamento - índice composto
        modelBuilder.Entity<VinculoSensorEquipamento>()
            .HasIndex(e => new { e.IdSensor, e.IdEquipamento, e.EstaAtivo });

        // RegraAlerta - precisão e índices
        modelBuilder.Entity<RegraAlerta>(entity =>
        {
            entity.Property(e => e.LimiteMinimo).HasPrecision(18, 4);
            entity.Property(e => e.LimiteMaximo).HasPrecision(18, 4);
            entity.Property(e => e.MargemErro).HasPrecision(18, 4);
            entity.HasIndex(e => new { e.IdSensor, e.EstaAtivo });
        });

        // EstadoAlerta - tipo especial JSONB e índice único
        modelBuilder.Entity<EstadoAlerta>(entity =>
        {
            entity.Property(e => e.JsonMedicoesRecentes).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.IdSensor, e.IdRegraAlerta }).IsUnique();
        });

        // HistoricoAlerta - precisão e índices
        modelBuilder.Entity<HistoricoAlerta>(entity =>
        {
            entity.Property(e => e.ValorDisparo).HasPrecision(18, 4);
            entity.HasIndex(e => e.DisparadoEm);
            entity.HasIndex(e => new { e.IdSensor, e.DisparadoEm });
        });
    }
}
