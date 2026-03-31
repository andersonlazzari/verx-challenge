using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Lancamentos.API.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Lancamentos.API.IntegrationTests
{
    public class LancamentosApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public LancamentosApiTests(CustomWebApplicationFactory factory)
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
        public async Task POST_LancamentoValido_Retorna_201()
        {
            AutenticarCliente();
            var payload = new { Tipo = "Credito", Valor = 100m, Descricao = "Venda integração" };

            var response = await _client.PostAsJsonAsync("/api/v1/lancamentos", payload);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task POST_LancamentoInvalido_Retorna_Erro()
        {
            AutenticarCliente();
            var payload = new { Tipo = "Credito", Valor = -10m, Descricao = "Valor negativo" };

            // A validação do domínio (DDD) levanta exceção que propaga pelo pipeline do TestHost
            var act = () => _client.PostAsJsonAsync("/api/v1/lancamentos", payload);

            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task POST_SemToken_Retorna_401()
        {
            _client.DefaultRequestHeaders.Authorization = null;
            var payload = new { Tipo = "Credito", Valor = 50m, Descricao = "Sem auth" };

            var response = await _client.PostAsJsonAsync("/api/v1/lancamentos", payload);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task POST_ComToken_Persiste_No_Banco()
        {
            AutenticarCliente();
            var payload = new { Tipo = "Debito", Valor = 75m, Descricao = "Pagamento integração" };

            var response = await _client.PostAsJsonAsync("/api/v1/lancamentos", payload);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Verifica persistência no banco InMemory
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
            var lancamentos = db.Lancamentos.ToList();
            lancamentos.Should().Contain(l => l.Descricao == "Pagamento integração");
        }
    }
}
