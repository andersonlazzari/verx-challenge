using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Mensageria.Events
{
    // O uso de 'record' garante imutabilidade
    public record LancamentoCriadoEvent(
        Guid Id,
        DateTime DataHora,
        string Tipo,
        decimal Valor
    )
    {
        // Construtor vazio exigido pelo MassTransit para a desserialização do JSON
        public LancamentoCriadoEvent() : this(Guid.Empty, DateTime.MinValue, string.Empty, 0) { }
    }
}
