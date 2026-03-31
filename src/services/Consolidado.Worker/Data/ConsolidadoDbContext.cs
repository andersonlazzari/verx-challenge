using Core.Dominio.Entities;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.Worker.Data
{
    public class ConsolidadoDbContext : DbContext
    {
        public ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options) : base(options) { }

        public DbSet<SaldoDiario> SaldosDiarios { get; set; }

        public DbSet<EventoProcessado> EventosProcessados { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SaldoDiario>(e =>
            {
                e.ToTable("SaldosDiarios");

                e.HasKey(s => s.DataReferencia);

                e.Property(s => s.ValorTotal).HasColumnType("decimal(18,2)").IsRequired();
                e.Property(s => s.UltimaAtualizacao).IsRequired();
            });

            modelBuilder.Entity<EventoProcessado>(e =>
            {
                e.ToTable("EventosProcessados");

                e.HasKey(ep => ep.EventoId);
                e.Property(ep => ep.DataProcessamento).IsRequired();
            });
        }
    }
}
