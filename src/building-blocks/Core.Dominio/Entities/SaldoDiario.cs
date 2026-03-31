using Core.Dominio.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Dominio.Entities
{
    public class SaldoDiario
    {
        
        public DateTime DataReferencia { get; private set; }
        public decimal ValorTotal { get; private set; }
        public DateTime UltimaAtualizacao { get; private set; }

        protected SaldoDiario() { }

        public SaldoDiario(DateTime dataReferencia)
        {
            DataReferencia = dataReferencia.Date;
            ValorTotal = 0;
            UltimaAtualizacao = DateTime.UtcNow;
        }

        public void ConsolidarLancamento(decimal valor, TipoLancamento tipo)
        {
            if (tipo == TipoLancamento.Credito)
                ValorTotal += valor;
            else
                ValorTotal -= valor;

            UltimaAtualizacao = DateTime.UtcNow;
        }
    }
}
