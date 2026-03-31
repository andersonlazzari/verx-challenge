using Consolidado.Worker.Consumers;
using Consolidado.Worker.Data;
using Core.Dominio.Entities;
using Core.Mensageria.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Consolidado.Worker.Tests
{
    public class LancamentoCriadoConsumerTests
    {
        private readonly ConsolidadoDbContext _dbContext;
        private readonly LancamentoCriadoConsumer _consumer;
        private readonly Mock<ILogger<LancamentoCriadoConsumer>> _loggerMock;

        public LancamentoCriadoConsumerTests()
        {
            var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ConsolidadoDbContext(options);
            _loggerMock = new Mock<ILogger<LancamentoCriadoConsumer>>();
            _consumer = new LancamentoCriadoConsumer(_loggerMock.Object, _dbContext);
        }

        private static Mock<ConsumeContext<LancamentoCriadoEvent>> CriarContextoMock(LancamentoCriadoEvent evento)
        {
            var mock = new Mock<ConsumeContext<LancamentoCriadoEvent>>();
            mock.Setup(c => c.Message).Returns(evento);
            return mock;
        }

        [Fact]
        public async Task Deve_Processar_Evento_E_Criar_Saldo()
        {
            var evento = new LancamentoCriadoEvent(Guid.NewGuid(), DateTime.UtcNow, "Credito", 200m);
            var contexto = CriarContextoMock(evento);

            await _consumer.Consume(contexto.Object);

            var saldo = await _dbContext.SaldosDiarios.FirstOrDefaultAsync();
            saldo.Should().NotBeNull();
            saldo!.ValorTotal.Should().Be(200m);

            var processado = await _dbContext.EventosProcessados.FirstOrDefaultAsync();
            processado.Should().NotBeNull();
            processado!.EventoId.Should().Be(evento.Id);
        }

        [Fact]
        public async Task Deve_Ignorar_Evento_Ja_Processado()
        {
            var eventoId = Guid.NewGuid();
            _dbContext.EventosProcessados.Add(new EventoProcessado(eventoId));
            await _dbContext.SaveChangesAsync();

            var evento = new LancamentoCriadoEvent(eventoId, DateTime.UtcNow, "Credito", 999m);
            var contexto = CriarContextoMock(evento);

            await _consumer.Consume(contexto.Object);

            var saldos = await _dbContext.SaldosDiarios.ToListAsync();
            saldos.Should().BeEmpty();
        }

        [Fact]
        public async Task Deve_Atualizar_Saldo_Existente()
        {
            var data = DateTime.UtcNow;
            var saldoExistente = new SaldoDiario(data);
            saldoExistente.ConsolidarLancamento(100m, Core.Dominio.Enums.TipoLancamento.Credito);
            _dbContext.SaldosDiarios.Add(saldoExistente);
            await _dbContext.SaveChangesAsync();

            var evento = new LancamentoCriadoEvent(Guid.NewGuid(), data, "Debito", 30m);
            var contexto = CriarContextoMock(evento);

            await _consumer.Consume(contexto.Object);

            var saldo = await _dbContext.SaldosDiarios.FirstAsync();
            saldo.ValorTotal.Should().Be(70m);
        }
    }
}
