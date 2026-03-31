using Consolidado.Worker.Data;
using Core.Dominio.Entities;
using Core.Dominio.Enums;
using Core.Mensageria.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.Worker.Consumers
{
    public class LancamentoCriadoConsumer : IConsumer<LancamentoCriadoEvent>
    {
        private readonly ILogger<LancamentoCriadoConsumer> _logger;
        private readonly ConsolidadoDbContext _dbContext;

        public LancamentoCriadoConsumer(ILogger<LancamentoCriadoConsumer> logger, ConsolidadoDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<LancamentoCriadoEvent> context)
        {
            var evento = context.Message;
            _logger.LogInformation("Recebendo evento de lanÃ§amento: {LancamentoId}", evento.Id);

            var jaProcessado = await _dbContext.EventosProcessados
                .AnyAsync(e => e.EventoId == evento.Id);

            if (jaProcessado)
            {
                _logger.LogWarning("Evento {LancamentoId} ignorado (IdempotÃªncia: jÃ¡ processado).", evento.Id);
                return;
            }

            var dataReferencia = evento.DataHora.Date;

            var saldo = await _dbContext.SaldosDiarios
                .FirstOrDefaultAsync(s => s.DataReferencia == dataReferencia);

            if (saldo == null)
            {
                saldo = new Core.Dominio.Entities.SaldoDiario(dataReferencia);
                _dbContext.SaldosDiarios.Add(saldo);
            }

            var tipoLancamento = Enum.Parse<TipoLancamento>(evento.Tipo);
            saldo.ConsolidarLancamento(evento.Valor, tipoLancamento);

            _dbContext.EventosProcessados.Add(new EventoProcessado(evento.Id));

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Saldo consolidado com sucesso para o evento {LancamentoId}.", evento.Id);
        }
    }
}
