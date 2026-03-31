# ADR 001: Adoção de EDA e Mensageria

## Contexto
O Ponto de Venda (PDV) é o módulo mais crítico da operação e exige alta disponibilidade contínua. Ele não pode sofrer degradação ou indisponibilidade caso o painel de consolidação de saldos enfrente intermitências ou picos de acesso.

## Decisão
A abordagem escolhida é a Arquitetura Orientada a Eventos (EDA) suportada por um Message Broker.
A API de Lançamentos reduz seu escopo apenas à persistência do log transacional local e à publicação do evento `LancamentoCriadoEvent`. Esse evento trafega o ID da operação, o tipo e o valor.

O Worker do Consolidado consome os eventos de forma assíncrona.
Um ponto de atenção na implementação: como o broker tem garantia de entrega "at-least-once", a lógica de consumo do Worker foi desenhada com controle de idempotência para descartar reentregas da mesma mensagem e proteger a integridade do saldo.

## Trade-offs

**Vantagens:** 
- Isolamento absoluto de falhas. A captação de transações financeiras opera protegida contra indisponibilidades do serviço de retaguarda.
- Margem para escalabilidade horizontal independente nos consumidores, o que é valioso para períodos sazonais de alto volume.

**Pontos de Atenção:** 
- Introduz o comportamento de consistência eventual (o saldo consolidado reflete a transação com milissegundos de atraso).
- Adiciona um componente de infraestrutura que exige monitoramento, gestão de Dead Letter Queues (DLQ) e estratégias adequadas de retry.

## Status
Aprovado