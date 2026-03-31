using Core.Dominio.Enums;
using Core.Mensageria.Events;
using FluentAssertions;
using Lancamentos.API.Controllers;
using Lancamentos.API.Data;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Lancamentos.API.Tests
{
    public class LancamentosControllerTests
    {
        private readonly LancamentosDbContext _dbContext;
        private readonly Mock<IPublishEndpoint> _publishEndpointMock;
        private readonly LancamentosController _controller;

        public LancamentosControllerTests()
        {
            var options = new DbContextOptionsBuilder<LancamentosDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new LancamentosDbContext(options);
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _controller = new LancamentosController(_dbContext, _publishEndpointMock.Object);
        }

        [Fact]
        public async Task Deve_Registrar_Lancamento_E_Retornar_Created()
        {
            var request = new LancamentoRequest("Credito", 100m, "Venda PDV");

            var result = await _controller.RegistrarLancamento(request);

            result.Should().BeOfType<CreatedAtActionResult>();
            _dbContext.Lancamentos.Should().HaveCount(1);
        }

        [Fact]
        public async Task Deve_Publicar_Evento_Com_Dados_Corretos()
        {
            LancamentoCriadoEvent? eventoCapturado = null;
            _publishEndpointMock
                .Setup(p => p.Publish(It.IsAny<LancamentoCriadoEvent>(), It.IsAny<CancellationToken>()))
                .Callback<LancamentoCriadoEvent, CancellationToken>((e, _) => eventoCapturado = e)
                .Returns(Task.CompletedTask);

            var request = new LancamentoRequest("Credito", 250m, "Venda cartão");

            await _controller.RegistrarLancamento(request);

            eventoCapturado.Should().NotBeNull();
            eventoCapturado!.Valor.Should().Be(250m);
            eventoCapturado.Tipo.Should().Be("Credito");
        }

        [Fact]
        public async Task Deve_Persistir_Lancamento_No_Banco()
        {
            var request = new LancamentoRequest("Debito", 75m, "Pagamento fornecedor");

            await _controller.RegistrarLancamento(request);

            var lancamento = await _dbContext.Lancamentos.FirstAsync();
            lancamento.Tipo.Should().Be(TipoLancamento.Debito);
            lancamento.Valor.Should().Be(75m);
            lancamento.Descricao.Should().Be("Pagamento fornecedor");
        }

        [Fact]
        public async Task Deve_Rejeitar_Tipo_Invalido()
        {
            var request = new LancamentoRequest("TipoInexistente", 10m, "Teste");

            var act = () => _controller.RegistrarLancamento(request);

            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
