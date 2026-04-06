using Core.Dominio.Entities;
using FluentAssertions;
using Xunit;

namespace Core.Dominio.Tests
{
    public class EventoProcessadoTests
    {
        [Fact]
        public void Deve_Criar_Com_Id_E_DataProcessamento()
        {
            var eventoId = Guid.NewGuid();

            var evento = new EventoProcessado(eventoId);

            evento.EventoId.Should().Be(eventoId);
            evento.DataProcessamento.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Dois_Eventos_Com_Mesmo_Id_Devem_Preservar_Identidade_Individual()
        {
            // Garante que dois registros distintos com o mesmo EventoId
            // não colapsam — o controle de idempotência é feito via query no banco,
            // não via igualdade de objetos em memória.
            var eventoId = Guid.NewGuid();

            var evento1 = new EventoProcessado(eventoId);
            var evento2 = new EventoProcessado(eventoId);

            evento1.Should().NotBeSameAs(evento2);
            evento1.EventoId.Should().Be(evento2.EventoId);
        }

        [Fact]
        public void DataProcessamento_Deve_Ser_Utc()
        {
            var eventoId = Guid.NewGuid();

            var evento = new EventoProcessado(eventoId);

            evento.DataProcessamento.Kind.Should().Be(DateTimeKind.Utc);
        }
    }
}

