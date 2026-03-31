# Domínios e Capacidades

O mapeamento abaixo reflete o isolamento dos contextos (Bounded Contexts) aplicados neste projeto, visando garantir resiliência onde o negócio mais exige e performance nas visões de gestão.

## 1. Frente de Caixa (PDV)
- **Função Core:** Registro imediato de entradas e saídas (vendas e estornos).
- **Importância de Negócio:** Módulo vital. Indisponibilidade aqui reflete diretamente em perda financeira pontual.
- **Implementação:** A API foi desenhada para atuar como um log de inserção rápida (append-only model), validando o lançamento básico e despachando o evento via Message Broker.

## 2. Gestão Financeira (Consolidado)
- **Função Core:** Agregação de dados para exibição de saldo diário.
- **Importância de Negócio:** Fornecer suporte decisório de baixa latência ao lojista, sem interferir na operação do fluxo primário (PDV).
- **Implementação:** O módulo opera exclusivamente com tabelas modeladas para leitura rápida, apoiadas por estratégias de cache em memória para suportar picos de 50 req/s.

---

## Decisões Práticas

- **Desacoplamento via Mensageria:** O isolamento dos sistemas garante que se a camada analítica (consolidado) sofrer downtime ou lentidão no backend relacional, a capacidade transacional da frente de caixa permanecerá íntegra.
- **Estratégia de Cache e Projeção:** Em vez de executar funções agregadoras complexas a cada listagem, introduzimos uma projeção atualizada pelo Worker que reflete o sumário por dia, e posicionamos um cache de ciclo curto sobre as consultas mais frequentes.
- **Garantia de Idempotência no Worker:** Mecanismos de tolerância a falhas em filas (re-delivery) não podem corromper as finanças da organização. O Worker avalia ativamente o UUID do evento base antes de efetivar o cálculo do saldo diário.

## Visão de Futuro e Evolução
O investimento na arquitetura reativa permite anexar novos módulos de suporte com o menor impacto possível na operação principal:
- Um serviço de backoffice para previsão de caixa D+1 conectado ao tráfego do mesmo broker.
- Um consumidor gerando rastreamento tático de prevenção a fraudes sobre transações críticas.
- Delegação assíncrona para motores de conciliação fiscal ou contábil.
