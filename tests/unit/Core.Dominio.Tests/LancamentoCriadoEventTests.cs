using Core.Mensageria.Events;
using FluentAssertions;
using Xunit;

namespace Core.Dominio.Tests
{
    /// <summary>
    /// Valida o contrato imutável do Integration Event.
    /// A premissa central é garantir que o payload publicado no barramento
    /// preserve fidelidade estrutural, independente do serviço que o consuma.
    /// </summary>
    public class LancamentoCriadoEventTests
    {
        [Fact]
        public void Deve_Criar_Evento_Com_Dados_Corretos()
        {
            var id = Guid.NewGuid();
            var dataHora = new DateTime(2026, 4, 1, 10, 30, 0, DateTimeKind.Utc);

            var evento = new LancamentoCriadoEvent(id, dataHora, "Credito", 250.75m);

            evento.Id.Should().Be(id);
            evento.DataHora.Should().Be(dataHora);
            evento.Tipo.Should().Be("Credito");
            evento.Valor.Should().Be(250.75m);
        }

        [Fact]
        public void Deve_Criar_Evento_Debito_Com_Dados_Corretos()
        {
            var id = Guid.NewGuid();
            var dataHora = DateTime.UtcNow;

            var evento = new LancamentoCriadoEvent(id, dataHora, "Debito", 100m);

            evento.Tipo.Should().Be("Debito");
            evento.Valor.Should().Be(100m);
        }

        [Fact]
        public void Construtor_Vazio_Deve_Produzir_Estado_Neutro()
        {
            // O construtor sem parâmetros é exigido pelo MassTransit para a
            // desserialização do payload JSON recebido do barramento.
            var evento = new LancamentoCriadoEvent();

            evento.Id.Should().Be(Guid.Empty);
            evento.DataHora.Should().Be(DateTime.MinValue);
            evento.Tipo.Should().BeEmpty();
            evento.Valor.Should().Be(0m);
        }

        [Fact]
        public void Dois_Eventos_Com_Mesmos_Dados_Devem_Ser_Iguais()
        {
            // Records em C# possuem igualdade estrutural por valor,
            // o que garante a comparação correta de eventos em cenários de idempotência.
            var id = Guid.NewGuid();
            var dataHora = DateTime.UtcNow;

            var evento1 = new LancamentoCriadoEvent(id, dataHora, "Credito", 300m);
            var evento2 = new LancamentoCriadoEvent(id, dataHora, "Credito", 300m);

            evento1.Should().Be(evento2);
        }

        [Fact]
        public void Dois_Eventos_Com_Ids_Diferentes_Devem_Ser_Distintos()
        {
            var dataHora = DateTime.UtcNow;

            var evento1 = new LancamentoCriadoEvent(Guid.NewGuid(), dataHora, "Credito", 300m);
            var evento2 = new LancamentoCriadoEvent(Guid.NewGuid(), dataHora, "Credito", 300m);

            evento1.Should().NotBe(evento2);
        }
    }
}
