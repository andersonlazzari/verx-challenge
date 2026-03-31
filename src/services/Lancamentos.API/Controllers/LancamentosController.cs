using Core.Dominio.Entities;
using Core.Dominio.Enums;
using Core.Mensageria.Events;
using Lancamentos.API.Data;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lancamentos.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LancamentosController : ControllerBase
    {
        private readonly LancamentosDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;

        public LancamentosController(LancamentosDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarLancamento([FromBody] LancamentoRequest request)
        {
            var tipo = Enum.Parse<TipoLancamento>(request.Tipo, true);
            var lancamento = new Lancamento(tipo, request.Valor, request.Descricao);

            _context.Lancamentos.Add(lancamento);
            await _context.SaveChangesAsync();

            var evento = new LancamentoCriadoEvent(
                lancamento.Id,
                lancamento.DataHora,
                lancamento.Tipo.ToString(),
                lancamento.Valor
            );

            await _publishEndpoint.Publish(evento);

            return CreatedAtAction(nameof(RegistrarLancamento), new { id = lancamento.Id }, lancamento);
        }
    }

    public record LancamentoRequest(string Tipo, decimal Valor, string Descricao);
}
