using Consolidado.Worker.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Consolidado.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ConsolidadoController : ControllerBase
    {
        private readonly ConsolidadoDbContext _context;
        private readonly IMemoryCache _cache;

        public ConsolidadoController(ConsolidadoDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet("{data}")]
        public async Task<IActionResult> ObterSaldoDiario(DateTime data)
        {
            var dataBusca = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
            var cacheKey = $"saldo_{dataBusca:yyyyMMdd}";

            if (_cache.TryGetValue(cacheKey, out decimal saldoEmCache))
            {
                return Ok(new { Data = dataBusca, Saldo = saldoEmCache, Origem = "Cache" });
            }

            var saldo = await _context.SaldosDiarios
                .AsNoTracking() // Desliga o rastreamento, aumentando a alocaÃ§Ã£o de acesso a leitura
                .FirstOrDefaultAsync(s => s.DataReferencia == dataBusca);

            var valorSaldo = saldo?.ValorTotal ?? 0m;

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(10));

            _cache.Set(cacheKey, valorSaldo, cacheOptions);

            return Ok(new { Data = dataBusca, Saldo = valorSaldo, Origem = "Database" });
        }
    }
}
