using Core.Dominio.Entities;
using Core.Dominio.Enums;
using FluentAssertions;
using Xunit;

namespace Core.Dominio.Tests
{
    public class SaldoDiarioTests
    {
        [Fact]
        public void Deve_Iniciar_Com_Saldo_Zerado()
        {
            var saldo = new SaldoDiario(DateTime.UtcNow);

            saldo.ValorTotal.Should().Be(0m);
            saldo.DataReferencia.Should().Be(DateTime.UtcNow.Date);
        }

        [Fact]
        public void Deve_Somar_Credito_Ao_Saldo()
        {
            var saldo = new SaldoDiario(DateTime.UtcNow);

            saldo.ConsolidarLancamento(200m, TipoLancamento.Credito);

            saldo.ValorTotal.Should().Be(200m);
        }

        [Fact]
        public void Deve_Subtrair_Debito_Do_Saldo()
        {
            var saldo = new SaldoDiario(DateTime.UtcNow);

            saldo.ConsolidarLancamento(50m, TipoLancamento.Debito);

            saldo.ValorTotal.Should().Be(-50m);
        }

        [Fact]
        public void Deve_Consolidar_Multiplos_Lancamentos()
        {
            var saldo = new SaldoDiario(DateTime.UtcNow);

            saldo.ConsolidarLancamento(500m, TipoLancamento.Credito);
            saldo.ConsolidarLancamento(200m, TipoLancamento.Debito);
            saldo.ConsolidarLancamento(100m, TipoLancamento.Credito);
            saldo.ConsolidarLancamento(50m, TipoLancamento.Debito);

            saldo.ValorTotal.Should().Be(350m);
        }

        [Fact]
        public void Deve_Atualizar_Data_Ultima_Atualizacao_Ao_Consolidar()
        {
            var saldo = new SaldoDiario(DateTime.UtcNow);
            var antesConsolidacao = saldo.UltimaAtualizacao;

            saldo.ConsolidarLancamento(100m, TipoLancamento.Credito);

            saldo.UltimaAtualizacao.Should().BeOnOrAfter(antesConsolidacao);
        }
    }
}
