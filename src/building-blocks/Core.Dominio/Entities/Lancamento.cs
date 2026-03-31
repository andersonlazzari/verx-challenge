using Core.Dominio.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Dominio.Entities
{
    public class Lancamento
    {
        public Guid Id { get; private set; }
        public DateTime DataHora { get; private set; }
        public TipoLancamento Tipo { get; private set; }
        public decimal Valor { get; private set; }
        public string Descricao { get; private set; }
                
        protected Lancamento() { }

        public Lancamento(TipoLancamento tipo, decimal valor, string descricao)
        {
            if (valor <= 0)
                throw new ArgumentException("O valor do lançamento deve ser maior que zero.");

            if (string.IsNullOrWhiteSpace(descricao))
                throw new ArgumentException("A descrição é obrigatória.");

            Id = Guid.NewGuid();
            DataHora = DateTime.UtcNow;
            Tipo = tipo;
            Valor = valor;
            Descricao = descricao;
        }
    }
}
