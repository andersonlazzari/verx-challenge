using Core.Dominio.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.API.Data
{
    public class LancamentosDbContext : DbContext
    {
        public LancamentosDbContext(DbContextOptions<LancamentosDbContext> options) : base(options) { }

        public DbSet<Lancamento> Lancamentos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lancamento>(e =>
            {
                e.ToTable("Lancamentos");
                e.HasKey(l => l.Id);

                e.Property(l => l.Valor).HasColumnType("decimal(18,2)").IsRequired();

                e.Property(l => l.Tipo).HasConversion<string>().IsRequired();

                e.Property(l => l.Descricao).HasMaxLength(250).IsRequired();
                e.Property(l => l.DataHora).IsRequired();
            });
        }
    }
}
