using Core.Dominio.Entities;
using Core.Dominio.Enums;
using FluentAssertions;
using Xunit;

namespace Core.Dominio.Tests
{
    public class LancamentoTests
    {
        [Fact]
        public void Deve_Criar_Lancamento_Credito_Com_Sucesso()
        {
            var lancamento = new Lancamento(TipoLancamento.Credito, 150.50m, "Venda em dinheiro");

            lancamento.Id.Should().NotBeEmpty();
            lancamento.Tipo.Should().Be(TipoLancamento.Credito);
            lancamento.Valor.Should().Be(150.50m);
            lancamento.Descricao.Should().Be("Venda em dinheiro");
            lancamento.DataHora.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Deve_Criar_Lancamento_Debito_Com_Sucesso()
        {
            var lancamento = new Lancamento(TipoLancamento.Debito, 75.00m, "Pagamento fornecedor");

            lancamento.Tipo.Should().Be(TipoLancamento.Debito);
            lancamento.Valor.Should().Be(75.00m);
            lancamento.Descricao.Should().Be("Pagamento fornecedor");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100.50)]
        public void Deve_Rejeitar_Valor_Zero_Ou_Negativo(decimal valorInvalido)
        {
            var act = () => new Lancamento(TipoLancamento.Credito, valorInvalido, "Teste");

            act.Should().Throw<ArgumentException>()
                .WithMessage("*valor*maior que zero*");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Deve_Rejeitar_Descricao_Vazia(string? descricaoInvalida)
        {
            var act = () => new Lancamento(TipoLancamento.Credito, 10m, descricaoInvalida!);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*descri*obrigat*");
        }
    }
}
