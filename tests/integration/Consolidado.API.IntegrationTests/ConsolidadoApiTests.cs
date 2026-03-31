using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Consolidado.Worker.Data;
using Core.Dominio.Entities;
using Core.Dominio.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Consolidado.API.IntegrationTests
{
    public class ConsolidadoApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public ConsolidadoApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private static string GerarTokenJwt()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("EstaEhUmaChaveSuperSecretaParaODesafioVerx2026");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private void AutenticarCliente()
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", GerarTokenJwt());
        }

        [Fact]
        public async Task GET_SaldoExistente_Retorna_200_Com_Valor()
        {
            // Prepara um saldo no banco InMemory
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
                var data = DateTime.UtcNow.Date;
                var saldo = new SaldoDiario(data);
                saldo.ConsolidarLancamento(500m, TipoLancamento.Credito);
                db.SaldosDiarios.Add(saldo);
                await db.SaveChangesAsync();
            }

            AutenticarCliente();
            var dataFormatada = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

            var response = await _client.GetAsync($"/api/v1/consolidado/{dataFormatada}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("500");
        }

        [Fact]
        public async Task GET_SaldoInexistente_Retorna_200_Com_Zero()
        {
            AutenticarCliente();

            var response = await _client.GetAsync("/api/v1/consolidado/2020-01-01");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("0");
        }

        [Fact]
        public async Task GET_SegundaRequisicao_Retorna_Do_Cache()
        {
            // Prepara um saldo no banco InMemory
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
                var data = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
                var saldo = new SaldoDiario(data);
                saldo.ConsolidarLancamento(300m, TipoLancamento.Credito);
                db.SaldosDiarios.Add(saldo);
                await db.SaveChangesAsync();
            }

            AutenticarCliente();

            // Primeira requisição — busca do banco (Origem: Database)
            var response1 = await _client.GetAsync("/api/v1/consolidado/2025-06-15");
            var body1 = await response1.Content.ReadAsStringAsync();
            body1.Should().Contain("Database");

            // Segunda requisição — retorna do cache (Origem: Cache)
            var response2 = await _client.GetAsync("/api/v1/consolidado/2025-06-15");
            var body2 = await response2.Content.ReadAsStringAsync();
            body2.Should().Contain("Cache");
        }
    }
}
