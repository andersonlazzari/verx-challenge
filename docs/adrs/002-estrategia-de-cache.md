# ADR 002: Estratégia de Cache para o Consolidado

## Contexto
O requisito de negócio estabelece que a API de consolidação suporte 50 requisições por segundo. Consultar diretamente o banco relacional para realizar funções de agregação exauriria o pool de conexões rapidamente em momentos de estresse do sistema.

## Decisão
Optamos por implementar controle de cache na própria camada de aplicação (via `IMemoryCache` no ecossistema .NET) como primeira linha de defesa, antes de justificar a complexidade de um cluster de cache distribuído.
A API retém a leitura do saldo consolidado em memória por uma janela curta (TTL de 10 a 15 segundos). Após a expiração, a aplicação consulta a base otimizada de leitura, atualiza os valores e renova a chave.

## Trade-offs

**Vantagens:**
- Tempo de resposta extremamente baixo (inferior a 1ms) para o volume principal de acessos, aliviando a carga sobre o banco de dados.
- Alinhamento forte com a cultura FinOps, evitando o provisionamento e o custo de licenciamento de um cluster externo dedicado (como Redis) nesse momento da arquitetura.

**Pontos de Atenção:**
- Efeito de "cold start" após o deploy ou reinicialização de pods: as requisições iniciais sofrem a latência normal da ida ao banco até que o cache aqueça.
- Em um cenário de escalabilidade com múltiplas instâncias da API, o cache permanece em escopo local. Isso pode gerar inconsistências temporais ligeiras entre requests balanceados para nós diferentes (inconsistência rápida, que consideramos um trade-off aceitável para o cenário).

## Status
Aprovado