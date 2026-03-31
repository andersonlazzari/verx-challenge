using Consolidado.API.Controllers;
using Consolidado.Worker.Data;
using Core.Dominio.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Consolidado.API.Tests
{
    public class ConsolidadoControllerTests
    {
        private readonly ConsolidadoDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly ConsolidadoController _controller;

        public ConsolidadoControllerTests()
        {
            var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ConsolidadoDbContext(options);
            _cache = new MemoryCache(new MemoryCacheOptions());
            _controller = new ConsolidadoController(_dbContext, _cache);
        }

        [Fact]
        public async Task Deve_Retornar_Saldo_Do_Cache_Quando_Existir()
        {
            var data = DateTime.UtcNow.Date;
            var cacheKey = $"saldo_{data:yyyyMMdd}";
            _cache.Set(cacheKey, 500m, TimeSpan.FromSeconds(30));

            var result = await _controller.ObterSaldoDiario(data) as OkObjectResult;

            result.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
            json.Should().Contain("Cache");
        }

        [Fact]
        public async Task Deve_Consultar_Banco_E_Cachear_Quando_Nao_Existir_No_Cache()
        {
            var data = DateTime.UtcNow.Date;
            var saldo = new SaldoDiario(data);
            saldo.ConsolidarLancamento(300m, Core.Dominio.Enums.TipoLancamento.Credito);
            _dbContext.SaldosDiarios.Add(saldo);
            await _dbContext.SaveChangesAsync();

            var result = await _controller.ObterSaldoDiario(data) as OkObjectResult;

            result.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
            json.Should().Contain("Database");
            json.Should().Contain("300");
        }

        [Fact]
        public async Task Deve_Retornar_Zero_Quando_Nao_Houver_Saldo()
        {
            var data = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = await _controller.ObterSaldoDiario(data) as OkObjectResult;

            result.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
            json.Should().Contain("0");
        }
    }
}
