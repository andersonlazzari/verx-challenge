using Lancamentos.API.Data;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lancamentos.API.IntegrationTests
{
    /// <summary>
    /// Factory customizada que substitui a infraestrutura real (PostgreSQL + RabbitMQ)
    /// por implementações em memória, isolando os testes de dependências externas.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        // Nome fixo por instância da factory para garantir que todos os scopes usem o mesmo banco
        private readonly string _dbName = "LancamentosTestDb_" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove TODOS os registros relacionados ao EF Core (Npgsql)
                var efDescriptors = services
                    .Where(d => d.ServiceType.FullName != null &&
                                (d.ServiceType.FullName.Contains("EntityFrameworkCore") ||
                                 d.ServiceType.FullName.Contains("DbContextOptions") ||
                                 d.ServiceType == typeof(LancamentosDbContext)))
                    .ToList();
                foreach (var d in efDescriptors) services.Remove(d);

                // Re-adiciona com InMemory Database (nome fixo por instância)
                services.AddDbContext<LancamentosDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));

                // Substitui o transporte do MassTransit (RabbitMQ -> InMemory)
                services.AddMassTransitTestHarness();
            });
        }
    }
}
