using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Dominio.Entities
{
    public class EventoProcessado
    {
        public Guid EventoId { get; private set; } 
        public DateTime DataProcessamento { get; private set; }

        protected EventoProcessado() { }

        public EventoProcessado(Guid eventoId)
        {
            EventoId = eventoId;
            DataProcessamento = DateTime.UtcNow;
        }
    }
}
