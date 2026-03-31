using Consolidado.Worker.Consumers;
using Consolidado.Worker.Data;
using Core.Dominio.Entities;
using Core.Mensageria.Events;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Consolidado.Worker.IntegrationTests
{
    public class WorkerIntegrationTests : IAsyncLifetime
    {
        private ServiceProvider _provider = null!;
        private ITestHarness _harness = null!;
        private readonly string _dbName = "WorkerTestDb_" + Guid.NewGuid();

        public async Task InitializeAsync()
        {
            var services = new ServiceCollection();

            // Configura EF Core InMemory com nome estável para a instância do teste
            services.AddDbContext<ConsolidadoDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Configura MassTransit InMemory com o Consumer real
            services.AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<LancamentoCriadoConsumer>();
            });

            // Registra o Logger
            services.AddLogging();

            _provider = services.BuildServiceProvider();
            _harness = _provider.GetRequiredService<ITestHarness>();

            await _harness.Start();
        }

        public async Task DisposeAsync()
        {
            await _harness.Stop();
            await _provider.DisposeAsync();
        }

        [Fact]
        public async Task Evento_Publicado_Deve_Ser_Consumido_E_Consolidar_Saldo()
        {
            var eventoId = Guid.NewGuid();
            var dataFixa = new DateTime(2025, 10, 20, 10, 0, 0, DateTimeKind.Utc);
            var evento = new LancamentoCriadoEvent(eventoId, dataFixa, "Credito", 250m);

            // Publica o evento no bus em memória
            await _harness.Bus.Publish(evento);

            // Aguarda o Consumer processar (usando o ConsumerHarness específico)
            var consumerHarness = _harness.GetConsumerHarness<LancamentoCriadoConsumer>();
            
            (await consumerHarness.Consumed.Any<LancamentoCriadoEvent>(x => x.Context.Message.Id == eventoId))
                .Should().BeTrue("O LancamentoCriadoConsumer deve consumir a mensagem");

            // Verifica resultados usando um novo escopo para garantir que lemos do banco "fresco"
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();

            var saldo = await db.SaldosDiarios.FirstOrDefaultAsync(s => s.DataReferencia == dataFixa.Date);
            
            saldo.Should().NotBeNull("Deve haver um registro de saldo no banco para a data " + dataFixa.Date);
            saldo!.ValorTotal.Should().Be(250m);

            var processado = await db.EventosProcessados.FirstOrDefaultAsync(e => e.EventoId == eventoId);
            processado.Should().NotBeNull("O evento deve estar marcado como processado");
        }

        [Fact]
        public async Task Evento_Duplicado_Nao_Altera_Saldo()
        {
            var eventoId = Guid.NewGuid();

            // Marca o evento como já processado usando um escopo novo
            using (var scopeSeed = _provider.CreateScope())
            {
                var dbSeed = scopeSeed.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
                dbSeed.EventosProcessados.Add(new EventoProcessado(eventoId));
                await dbSeed.SaveChangesAsync();
            }

            // Publica o mesmo evento
            var evento = new LancamentoCriadoEvent(eventoId, DateTime.UtcNow, "Credito", 999m);
            await _harness.Bus.Publish(evento);

            // Aguarda o Consumer processar
            var consumerHarness = _harness.GetConsumerHarness<LancamentoCriadoConsumer>();
            (await consumerHarness.Consumed.Any<LancamentoCriadoEvent>(x => x.Context.Message.Id == eventoId))
                .Should().BeTrue("O evento deve ser consumido (mesmo que ignorado internamente)");

            // Verifica que NENHUM saldo foi criado (idempotência) usando um escopo novo
            using var scopeVerify = _provider.CreateScope();
            var dbVerify = scopeVerify.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
            
            var saldos = await dbVerify.SaldosDiarios.ToListAsync();
            saldos.Should().BeEmpty("Não deve haver saldo criado para um evento duplicado");
        }
    }
}
