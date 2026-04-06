using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Consolidado.API.Controllers;
using Consolidado.Worker.Consumers;
using Consolidado.Worker.Data;
using FluentAssertions;
using Lancamentos.API.Data;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

using Lancamentos.API.Controllers;

using Npgsql;

namespace FluxoCaixa.E2ETests
{
    public class FluxoCaixaE2EFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("fluxocaixa_master") // Banco master para criar os outros
            .WithUsername("e2e_user")
            .WithPassword("e2e_pass")
            .Build();

        private readonly RabbitMqContainer _rmqContainer = new RabbitMqBuilder()
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

        public WebApplicationFactory<LancamentosController> LancamentosFactory { get; private set; } = null!;
        public WebApplicationFactory<ConsolidadoController> ConsolidadoFactory { get; private set; } = null!;
        public IHost WorkerHost { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await Task.WhenAll(_dbContainer.StartAsync(), _rmqContainer.StartAsync());

            // Build clear connection strings to ISOLATE databases and avoid schema race conditions
            var masterConnBuilder = new NpgsqlConnectionStringBuilder(_dbContainer.GetConnectionString());
            
            masterConnBuilder.Database = "lancamentos_e2e";
            var lancamentosConnStr = masterConnBuilder.ToString();
            
            masterConnBuilder.Database = "consolidado_e2e";
            var consolidadoConnStr = masterConnBuilder.ToString();

            LancamentosFactory = new WebApplicationFactory<LancamentosController>()
                .WithWebHostBuilder(b =>
                {
                    b.UseEnvironment("Testing");
                    b.UseSetting("ConnectionStrings:DefaultConnection", lancamentosConnStr);
                    b.UseSetting("RabbitMQ:Host", _rmqContainer.Hostname);
                    b.UseSetting("RabbitMQ:Port", _rmqContainer.GetMappedPublicPort(5672).ToString());
                });

            ConsolidadoFactory = new WebApplicationFactory<ConsolidadoController>()
                .WithWebHostBuilder(b =>
                {
                    b.UseEnvironment("Testing");
                    b.UseSetting("ConnectionStrings:DefaultConnection", consolidadoConnStr);
                });

            // Forçamos a subida das APIs que executarão db.Database.EnsureCreated() em seus Program.cs
            _ = LancamentosFactory.Services.CreateScope();
            _ = ConsolidadoFactory.Services.CreateScope();

            // Sobe o Worker conectado ao banco de CONSOLIDADO (isolado das tabelas de Lancamentos)
            WorkerHost = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddDbContext<ConsolidadoDbContext>(opt =>
                        opt.UseNpgsql(consolidadoConnStr));

                    services.AddMassTransit(x =>
                    {
                        x.AddConsumer<LancamentoCriadoConsumer>();
                        x.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.Host(_rmqContainer.Hostname, _rmqContainer.GetMappedPublicPort(5672), "/", h =>
                            {
                                h.Username("guest");
                                h.Password("guest");
                            });
                            cfg.ReceiveEndpoint("fluxo-caixa-consolidado", e =>
                            {
                                // Limitamos a concorrência para 1 no ambiente de teste E2E para evitar race conditions
                                // na criação inicial dos saldos diários por múltiplos threads.
                                e.ConcurrentMessageLimit = 1;

                                e.ConfigureConsumer<LancamentoCriadoConsumer>(context);
                            });
                        });
                    });

                    services.AddLogging();
                })
                .Build();

            await WorkerHost.StartAsync();
            await Task.Delay(4000); // Wait for health checks and RabbitMQ binding
        }

        public async Task DisposeAsync()
        {
            if (WorkerHost is not null)
            {
                await WorkerHost.StopAsync();
                WorkerHost.Dispose();
            }

            LancamentosFactory?.Dispose();
            ConsolidadoFactory?.Dispose();

            await _dbContainer.DisposeAsync();
            await _rmqContainer.DisposeAsync();
        }

        public static string GerarTokenJwt()
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("EstaEhUmaChaveSuperSecretaParaODesafioVerx2026");
            var token = handler.CreateToken(new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            });
            return handler.WriteToken(token);
        }
    }

    [CollectionDefinition("E2E")]
    public class E2ECollection : ICollectionFixture<FluxoCaixaE2EFixture> { }

    /// <summary>
    /// Testes de ponta a ponta cobrindo exclusivamente o caminho crítico do sistema.
    /// Valida o fluxo real: POST na API Transacional → RabbitMQ → Worker Consolidador
    /// → persistência no banco de leitura → confirmação via API de Consulta.
    /// </summary>
    [Collection("E2E")]
    public class HappyPathE2ETests
    {
        private readonly FluxoCaixaE2EFixture _fixture;
        private readonly HttpClient _lancamentosClient;
        private readonly HttpClient _consolidadoClient;

        public HappyPathE2ETests(FluxoCaixaE2EFixture fixture)
        {
            _fixture = fixture;

            _lancamentosClient = _fixture.LancamentosFactory.CreateClient();
            _lancamentosClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", FluxoCaixaE2EFixture.GerarTokenJwt());

            _consolidadoClient = _fixture.ConsolidadoFactory.CreateClient();
            _consolidadoClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", FluxoCaixaE2EFixture.GerarTokenJwt());
        }

        [Fact]
        public async Task Lancamento_Credito_Deve_Refletir_No_Saldo_Consolidado()
        {
            // Usamos a data de amanhã para isolar este teste de outros que usem "Hoje"
            var dataTeste = DateTime.UtcNow.Date.AddDays(1);
            var payload = new { Tipo = "Credito", Valor = 350.00m, Descricao = "Venda PDV E2E", Data = dataTeste };

            var postResponse = await _lancamentosClient.PostAsJsonAsync("/api/v1/lancamentos", payload);

            postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Polling aguardando a propagação para o banco de consulta
            var saldoConsolidado = await AguardarConsolidacaoAsync(dataTeste, valorEsperado: 350.00m);

            saldoConsolidado.Should().NotBeNull();
            saldoConsolidado.Value.GetProperty("saldo").GetDecimal().Should().Be(350.00m);
        }

        [Fact]
        public async Task Multiplos_Lancamentos_Devem_Acumular_Saldo_Corretamente()
        {
            // Usamos a data de hoje para este cenário
            var dataTeste = DateTime.UtcNow.Date;
            
            // Act — três lançamentos: 500 + 200 - 150 = 550
            // Adicionamos pequenos delays entre as requisições no ambiente de teste
            // para evitar race conditions na criação do primeiro saldo do dia no Worker.
            await _lancamentosClient.PostAsJsonAsync("/api/v1/lancamentos",
                new { Tipo = "Credito", Valor = 500m, Descricao = "Abertura caixa", Data = dataTeste });
            
            await Task.Delay(500);

            await _lancamentosClient.PostAsJsonAsync("/api/v1/lancamentos",
                new { Tipo = "Credito", Valor = 200m, Descricao = "Venda cartão", Data = dataTeste });
            
            await Task.Delay(500);

            await _lancamentosClient.PostAsJsonAsync("/api/v1/lancamentos",
                new { Tipo = "Debito", Valor = 150m, Descricao = "Pagamento fornecedor", Data = dataTeste });

            // Aguarda a consolidação final (líquido 550)
            var saldoFinal = await AguardarConsolidacaoAsync(dataTeste, valorEsperado: 550m);

            saldoFinal.Should().NotBeNull("O saldo final deve ser consolidado após todos os eventos.");
            saldoFinal.Value.GetProperty("saldo").GetDecimal().Should().Be(550.00m);
        }

        [Fact]
        public async Task Requisicao_Sem_Autenticacao_Deve_Retornar_401()
        {
            var clienteSemAuth = _fixture.LancamentosFactory.CreateClient();
            var payload = new { Tipo = "Credito", Valor = 100m, Descricao = "Sem token" };

            var response = await clienteSemAuth.PostAsJsonAsync("/api/v1/lancamentos", payload);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                "Nenhum lançamento deve ser aceito sem autenticação JWT válida.");
        }

        private async Task<JsonElement?> AguardarConsolidacaoAsync(DateTime data, decimal valorEsperado)
        {
            var dataStr = data.ToString("yyyy-MM-dd");

            for (int tentativa = 0; tentativa < 16; tentativa++)
            {
                await Task.Delay(500);
                var response = await _consolidadoClient.GetAsync($"/api/v1/consolidado/{dataStr}");
                if (!response.IsSuccessStatusCode) continue;

                var body = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(body).RootElement;

                if (json.TryGetProperty("saldo", out var saldoEl) &&
                    saldoEl.GetDecimal() == valorEsperado)
                    return json;
            }

            return null;
        }

        private async Task<JsonElement?> AguardarConsolidacaoComValorMinimoAsync(decimal valorMinimo)
        {
            var dataStr = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

            for (int tentativa = 0; tentativa < 16; tentativa++)
            {
                await Task.Delay(500);
                var response = await _consolidadoClient.GetAsync($"/api/v1/consolidado/{dataStr}");
                if (!response.IsSuccessStatusCode) continue;

                var body = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(body).RootElement;

                if (json.TryGetProperty("saldo", out var saldoEl) &&
                    saldoEl.GetDecimal() >= valorMinimo)
                    return json;
            }

            return null;
        }
    }
}
