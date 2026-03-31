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
    }
}
